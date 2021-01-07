using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

namespace NeXt.Vdf
{
    /// <summary>
    /// Base class for al Deserialization exceptions
    /// </summary>
    public class VdfDeserializationException : Exception
    {
        public VdfDeserializationException() : base() { }
        public VdfDeserializationException(string message) : base() { }
        public VdfDeserializationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// A character was unexpected during deserialization
    /// </summary>
    public class UnexpectedCharacterException : VdfDeserializationException
    {
        public UnexpectedCharacterException(string message, char c) : base(message)
        {
            Character = c;
        }

        public char Character {get; private set;}
    }

    /// <summary>
    /// Deserializes Vdf formatted text into a VdfValue
    /// </summary>
    public class VdfDeserializer
    {
        /// <summary>
        /// Creates a VdfDeserializer object
        /// </summary>
        /// <param name="vdfText">the text to deserialize</param>
        public VdfDeserializer(string vdfText)
        {
            VdfText = vdfText;
        }

        /// <summary>
        /// Creates a vdfdeserializer object
        /// </summary>
        /// <param name="filePath">path to the file to deserialize</param>
        /// <returns></returns>
        public static VdfDeserializer FromFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                return new VdfDeserializer(reader.ReadToEnd());
            }            
        }

        private string VdfText;

        private enum TokenType
        {
            String,
            TableStart,
            TableEnd,
            Comment,
            None,
        }

        private struct Token
        {
            public TokenType type;
            public string content;
        }

        private enum CharacterType
        {
            Whitespace,
            Newline,
            SequenceDelimiter,
            CommentDelimiter,
            TableOpen,
            TableClose,
            EscapeChar,
            Char,
        }
        private CharacterType GetCharType(char c)
        {
            switch(c)
            {
                case '\n': return CharacterType.Newline;
                case '\r':
                case '\t':
                case ' ':
                {
                    return CharacterType.Whitespace;
                }

                case '{': return CharacterType.TableOpen;
                case '}': return CharacterType.TableClose;
                case '\\': return CharacterType.EscapeChar;
                case '/': return CharacterType.CommentDelimiter;
                case '"': return CharacterType.SequenceDelimiter;
                default: return CharacterType.Char;
            }
        }       


        /// <summary>
        /// gets the character a single escape char represents
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private char GetUnescapedChar(char c)
        {
            switch(c)
            {
                case 'n': return '\n';
                case 't': return '\t';
                default: return c;
            }
        }

        /// <summary>
        /// returns the next full text (or as much as possible if incomplete)
        /// </summary>
        /// <param name="s">text to search in</param>
        /// <param name="startindex">first index to look at</param>
        /// <param name="endIndex">the index the returned string ended at</param>
        /// <param name="fullStop">true if the string was over, false if it was incomplete</param>
        /// <returns></returns>
        private string GetTextToDelimiter(string s, int startindex, out int endIndex, out bool fullStop)
        {
            fullStop = true;
            bool openEscape = false;
            bool EscapedSequence = GetCharType(s[startindex]) == CharacterType.SequenceDelimiter;

            StringBuilder sb = new StringBuilder();
            for(int i = startindex; i < s.Length; i++)
            {
                switch(GetCharType(s[i]))
                {
                    case CharacterType.SequenceDelimiter:
                    {
                        if(!openEscape && EscapedSequence && i > startindex)
                        {
                            endIndex = i + 1;
                            return sb.ToString();
                        }
                        else if(!EscapedSequence)
                        {
                            throw new UnexpectedCharacterException("Non-Escape sequences cannot contain sequence delimiters", s[i]);
                        }
                        else if(openEscape)
                        {
                            sb.Append(GetUnescapedChar(s[i]));
                            openEscape = false;
                        }

                        break;
                    }
                    case CharacterType.Whitespace:
                    {
                        if(EscapedSequence)
                        {
                            sb.Append(s[i]);
                        }
                        else
                        {
                            endIndex = i;
                            return sb.ToString();
                        }
                        break;
                    }
                    case CharacterType.EscapeChar:
                    {
                        if(openEscape)
                        {
                            sb.Append(GetUnescapedChar(s[i]));
                        }
                        openEscape = !openEscape;
                        break;
                    }
                    default:
                    {
                        if(openEscape)
                        {
                            sb.Append(GetUnescapedChar(s[i]));
                            openEscape = false;
                        }
                        else
                        {
                            sb.Append(s[i]);
                        }
                        break;
                    }
                }
            }
            endIndex = s.Length;
            if(EscapedSequence)
            {
                if((GetCharType(s[s.Length-1]) != CharacterType.SequenceDelimiter))
                {
                    fullStop = false;
                }
                else
                {
                    int c = 0;
                    for (int i = s.Length - 2; i >= 0 && GetCharType(s[i]) == CharacterType.EscapeChar; i--)
                    {
                        c++;
                    }

                    fullStop = (c % 2) == 0;
                }
            }
            return sb.ToString();
        }

        private Token startedToken;
        private bool unclosedLine = false;
        private void HandleUnclosedLine(Action<Token> callback, string line)
        {
            int endindex;
            bool isEnd;
            string text = GetTextToDelimiter("\""+line, 0, out endindex, out isEnd);
            if (!isEnd)
            {
                startedToken.content += text;
                unclosedLine = true;
            }
            else
            {
                unclosedLine = false;
                callback(startedToken);
                if (endindex < line.Length)
                {
                    HandleLine(callback, line.Substring(endindex).Trim());
                }
            }
        }

        private void HandleLine(Action<Token> callback, string line)
        {
            if(string.IsNullOrEmpty(line))
            {
                return;
            }
            CharacterType ct = GetCharType(line[0]);
            switch(ct)
            {
                case CharacterType.TableOpen:
                {
                    callback(new Token(){type=TokenType.TableStart, content=line[0].ToString() });
                    break;
                }
                case CharacterType.TableClose:
                {
                    callback(new Token(){type=TokenType.TableEnd, content=line[0].ToString() });
                    break;
                }
                case CharacterType.CommentDelimiter:
                {
                    if(line.Length < 2 || GetCharType(line[1]) != CharacterType.CommentDelimiter)
                    {
                        throw new UnexpectedCharacterException("Single comment delimiter is not allowed", line[0]);
                    }
                    callback(new Token(){type=TokenType.Comment, content=line });         
                    break;
                }
                default:
                {
                    int endindex;
                    bool isEnd;
                    string text = GetTextToDelimiter(line, 0, out endindex, out isEnd);
                    if(!isEnd)
                    {
                        startedToken = new Token() { type = TokenType.String, content = text };
                        unclosedLine = true;
                    }
                    else
                    {
                        callback(new Token() { type = TokenType.String, content = text });
                        if (endindex < line.Length)
                        {
                            HandleLine(callback, line.Substring(endindex).Trim());
                        }
                    }
                    break;
                }
            }

        }

        /// <summary>
        /// Tokenizes the VdfFormatted string into a list of tokens
        /// </summary>
        /// <param name="s">the string to tokenize</param>
        /// <returns>the token list</returns>
        private List<Token> Tokenize(string s)
        {
            var result = new List<Token>();

            var lines = s.Split('\n').Select((v) => v.Trim());

            foreach(var line in lines)
            {
                if(unclosedLine)
                {
                    HandleUnclosedLine(result.Add, line);
                }
                else
                {
                    HandleLine(result.Add, line);
                }

            }

            return result;
        }

        /// <summary>
        /// Deserializes the Vdf string into a VdfValue
        /// </summary>
        /// <returns></returns>
        public VdfValue Deserialize()
        {
            if(VdfText== null)
            {
                throw new ArgumentNullException("s");
            }
            if(VdfText.Length < 1)
            {
                throw new ArgumentException("s cannot be empty ", "s");
            }

            var tokens = Tokenize(VdfText);

            if(tokens.Count < 1)
            {
                throw new ArgumentException("no tokens found in string", "s");
            }

            VdfValue root = null;
            VdfTable current = null;
            var comments = new List<string>();

            string name = null;

            foreach(var token in tokens)
            {
                if(token.type == TokenType.Comment)
                {
                    comments.Add(token.content.Substring(2));
                    continue;
                }


                if(root == null)
                {
                    if (token.type == TokenType.String)
                    {
                        if(name != null)
                        {
                            return new VdfString(name, token.content);
                        }

                        name = token.content;
                    }
                    else if (token.type == TokenType.TableStart)
                    {
                        root = new VdfTable(name);
                        if(comments.Count > 0)
                        {
                            foreach(var comment in comments)
                            {
                                root.Comments.Add(comment);
                            }
                            comments.Clear();
                        }
                        current = root as VdfTable;
                        name = null;
                    }
                    else
                    {
                        throw new VdfDeserializationException("Invalid format: First token was not a string");
                    }
                    continue;
                }

                if(name != null)
                {
                    VdfValue v;
                    if(token.type == TokenType.String)
                    {
                        long i;
                        decimal d;

                        if(long.TryParse(token.content, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
                        {
                            v = new VdfLong(name, i);
                        }
                        else if(decimal.TryParse(token.content, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                        {
                            v = new VdfDecimal(name, d);
                        }
                        else
                        {
                            v = new VdfString(name, token.content);
                        }
                        if (comments.Count > 0)
                        {
                            foreach (var comment in comments)
                            {
                                v.Comments.Add(comment);
                            }
                            comments.Clear();
                        }
                        name = null;
                        current.Add(v);
                    }
                    else if (token.type == TokenType.TableStart)
                    {
                        v = new VdfTable(name);
                        if (comments.Count > 0)
                        {
                            foreach (var comment in comments)
                            {
                                v.Comments.Add(comment);
                            }
                            comments.Clear();
                        }
                        current.Add(v);
                        name = null;
                        current = v as VdfTable;
                    }
                }
                else
                {
                    if(token.type == TokenType.String)
                    {
                        name = token.content;
                    }
                    else if(token.type == TokenType.TableEnd)
                    {
                        current = current.Parent as VdfTable;
                    }
                    else
                    {
                        throw new VdfDeserializationException("Invalid Format: a name was needed but not found");
                    }
                }
            }

            if(current != null)
            {
                throw new VdfDeserializationException("Invalid format: unclosed table");
            }

            return root;
        }
    }
}
