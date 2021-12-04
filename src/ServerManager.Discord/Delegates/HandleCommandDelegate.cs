﻿using ServerManagerTool.Discord.Enums;
using System.Collections.Generic;

namespace ServerManagerTool.Discord.Delegates
{
    public delegate IList<string> HandleCommandDelegate(CommandType commandType, string channelId, string profileId);
}