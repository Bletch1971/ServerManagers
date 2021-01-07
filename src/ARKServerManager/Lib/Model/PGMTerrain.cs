using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class PGMTerrain : AggregateIniValue
    {
        private new const char DELIMITER = ';';

        public PGMTerrain()
        {
            SnowBiomeLocation = new PGMTerrainXY(0.2f, 0.2f);
            RedWoodForestBiomeLocation = new PGMTerrainXY(0.5f, 0.5f);

            NorthRegion1Start = new PGMTerrainXY(0.25f, 0.0f);
            NorthRegion1End = new PGMTerrainXY(0.416f, 0.5f);
            NorthRegion2Start = new PGMTerrainXY(0.416f, 0.0f);
            NorthRegion2End = new PGMTerrainXY(0.582f, 0.5f);
            NorthRegion3Start = new PGMTerrainXY(0.582f, 0.0f);
            NorthRegion3End = new PGMTerrainXY(0.75f, 0.0f);
            SouthRegion1Start = new PGMTerrainXY(0.25f, 0.5f);
            SouthRegion1End = new PGMTerrainXY(0.416f, 1.0f);
            SouthRegion2Start = new PGMTerrainXY(0.416f, 0.5f);
            SouthRegion2End = new PGMTerrainXY(0.582f, 1.0f);
            SouthRegion3Start = new PGMTerrainXY(0.582f, 0.5f);
            SouthRegion3End = new PGMTerrainXY(0.75f, 1.0f);
            EastRegion1Start = new PGMTerrainXY(0.75f, 0.0f);
            EastRegion1End = new PGMTerrainXY(1.0f, 0.333f);
            EastRegion2Start = new PGMTerrainXY(0.75f, 0.333f);
            EastRegion2End = new PGMTerrainXY(1.0f, 0.666f);
            EastRegion3Start = new PGMTerrainXY(0.75f, 0.666f);
            EastRegion3End = new PGMTerrainXY(1.0f, 1.0f);
            WestRegion1Start = new PGMTerrainXY(0.0f, 0.0f);
            WestRegion1End = new PGMTerrainXY(0.25f, 0.333f);
            WestRegion2Start = new PGMTerrainXY(0.0f, 0.333f);
            WestRegion2End = new PGMTerrainXY(0.25f, 0.666f);
            WestRegion3Start = new PGMTerrainXY(0.0f, 0.666f);
            WestRegion3End = new PGMTerrainXY(0.25f, 1.0f);

            TerrainScaleMultiplier = new PGMTerrainXYZ(1.0f, 1.0f, 1.0f);
        }

        public static readonly DependencyProperty MapSeedProperty = DependencyProperty.Register(nameof(MapSeed), typeof(int), typeof(PGMTerrain), new PropertyMetadata(999));
        [DataMember]
        [AggregateIniValueEntry]
        public int MapSeed
        {
            get { return (int)GetValue(MapSeedProperty); }
            set { SetValue(MapSeedProperty, value); }
        }

        public static readonly DependencyProperty LandscapeRadiusProperty = DependencyProperty.Register(nameof(LandscapeRadius), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float LandscapeRadius
        {
            get { return (float)GetValue(LandscapeRadiusProperty); }
            set { SetValue(LandscapeRadiusProperty, value); }
        }

        public static readonly DependencyProperty WaterFrequencyProperty = DependencyProperty.Register(nameof(WaterFrequency), typeof(float), typeof(PGMTerrain), new PropertyMetadata(5.0f));
        [DataMember]
        [AggregateIniValueEntry(Key = "Water Frequency")]
        public float WaterFrequency
        {
            get { return (float)GetValue(WaterFrequencyProperty); }
            set { SetValue(WaterFrequencyProperty, value); }
        }

        public static readonly DependencyProperty MountainsFrequencyProperty = DependencyProperty.Register(nameof(MountainsFrequency), typeof(float), typeof(PGMTerrain), new PropertyMetadata(12.0f));
        [DataMember]
        [AggregateIniValueEntry(Key = "Mountains Frequency")]
        public float MountainsFrequency
        {
            get { return (float)GetValue(MountainsFrequencyProperty); }
            set { SetValue(MountainsFrequencyProperty, value); }
        }

        public static readonly DependencyProperty MountainsSlopeProperty = DependencyProperty.Register(nameof(MountainsSlope), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.8f));
        [DataMember]
        [AggregateIniValueEntry(Key = "Mountains Slope")]
        public float MountainsSlope
        {
            get { return (float)GetValue(MountainsSlopeProperty); }
            set { SetValue(MountainsSlopeProperty, value); }
        }

        public static readonly DependencyProperty MountainsHeightProperty = DependencyProperty.Register(nameof(MountainsHeight), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.25f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MountainsHeight
        {
            get { return (float)GetValue(MountainsHeightProperty); }
            set { SetValue(MountainsHeightProperty, value); }
        }

        public static readonly DependencyProperty TurbulencePowerProperty = DependencyProperty.Register(nameof(TurbulencePower), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.0125f));
        [DataMember]
        [AggregateIniValueEntry(Key = "Turbulence Power")]
        public float TurbulencePower
        {
            get { return (float)GetValue(TurbulencePowerProperty); }
            set { SetValue(TurbulencePowerProperty, value); }
        }

        public static readonly DependencyProperty ShoreSlopeProperty = DependencyProperty.Register(nameof(ShoreSlope), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry(Key = "Shore Slope")]
        public float ShoreSlope
        {
            get { return (float)GetValue(ShoreSlopeProperty); }
            set { SetValue(ShoreSlopeProperty, value); }
        }

        public static readonly DependencyProperty WaterLevelProperty = DependencyProperty.Register(nameof(WaterLevel), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-0.72f));
        [DataMember]
        [AggregateIniValueEntry]
        public float WaterLevel
        {
            get { return (float)GetValue(WaterLevelProperty); }
            set { SetValue(WaterLevelProperty, value); }
        }

        public static readonly DependencyProperty ShoreLineEndProperty = DependencyProperty.Register(nameof(ShoreLineEnd), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-0.715f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ShoreLineEnd
        {
            get { return (float)GetValue(ShoreLineEndProperty); }
            set { SetValue(ShoreLineEndProperty, value); }
        }

        public static readonly DependencyProperty GrassDensityProperty = DependencyProperty.Register(nameof(GrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float GrassDensity
        {
            get { return (float)GetValue(GrassDensityProperty); }
            set { SetValue(GrassDensityProperty, value); }
        }

        public static readonly DependencyProperty JungleGrassDensityProperty = DependencyProperty.Register(nameof(JungleGrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.02f));
        [DataMember]
        [AggregateIniValueEntry]
        public float JungleGrassDensity
        {
            get { return (float)GetValue(JungleGrassDensityProperty); }
            set { SetValue(JungleGrassDensityProperty, value); }
        }

        public static readonly DependencyProperty OceanFloorLevelProperty = DependencyProperty.Register(nameof(OceanFloorLevel), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float OceanFloorLevel
        {
            get { return (float)GetValue(OceanFloorLevelProperty); }
            set { SetValue(OceanFloorLevelProperty, value); }
        }

        public static readonly DependencyProperty SnowBiomeSizeProperty = DependencyProperty.Register(nameof(SnowBiomeSize), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.3f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowBiomeSize
        {
            get { return (float)GetValue(SnowBiomeSizeProperty); }
            set { SetValue(SnowBiomeSizeProperty, value); }
        }

        public static readonly DependencyProperty RedWoodBiomeSizeProperty = DependencyProperty.Register(nameof(RedWoodBiomeSize), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.075f));
        [DataMember]
        [AggregateIniValueEntry(Key = "RWBiomeSize")]
        public float RedWoodBiomeSize
        {
            get { return (float)GetValue(RedWoodBiomeSizeProperty); }
            set { SetValue(RedWoodBiomeSizeProperty, value); }
        }

        public static readonly DependencyProperty MountainBiomeStartProperty = DependencyProperty.Register(nameof(MountainBiomeStart), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-0.55f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MountainBiomeStart
        {
            get { return (float)GetValue(MountainBiomeStartProperty); }
            set { SetValue(MountainBiomeStartProperty, value); }
        }

        public static readonly DependencyProperty MountainsTreeDensityProperty = DependencyProperty.Register(nameof(MountainsTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.01f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MountainsTreeDensity
        {
            get { return (float)GetValue(MountainsTreeDensityProperty); }
            set { SetValue(MountainsTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty JungleBiomeStartProperty = DependencyProperty.Register(nameof(JungleBiomeStart), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-0.65f));
        [DataMember]
        [AggregateIniValueEntry]
        public float JungleBiomeStart
        {
            get { return (float)GetValue(JungleBiomeStartProperty); }
            set { SetValue(JungleBiomeStartProperty, value); }
        }

        public static readonly DependencyProperty IslandBorderCurveExponentProperty = DependencyProperty.Register(nameof(IslandBorderCurveExponent), typeof(float), typeof(PGMTerrain), new PropertyMetadata(4.0f));
        [DataMember]
        [AggregateIniValueEntry(Key = "IslandBorderCurveExp")]
        public float IslandBorderCurveExponent
        {
            get { return (float)GetValue(IslandBorderCurveExponentProperty); }
            set { SetValue(IslandBorderCurveExponentProperty, value); }
        }

        public static readonly DependencyProperty MaxSpawnPointHeightProperty = DependencyProperty.Register(nameof(MaxSpawnPointHeight), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.1f));
        [DataMember]
        [AggregateIniValueEntry(Key = "MaxSawnPointHeight")]
        public float MaxSpawnPointHeight
        {
            get { return (float)GetValue(MaxSpawnPointHeightProperty); }
            set { SetValue(MaxSpawnPointHeightProperty, value); }
        }

        public static readonly DependencyProperty MountainGrassDensityProperty = DependencyProperty.Register(nameof(MountainGrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.05f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MountainGrassDensity
        {
            get { return (float)GetValue(MountainGrassDensityProperty); }
            set { SetValue(MountainGrassDensityProperty, value); }
        }

        public static readonly DependencyProperty SnowMountainGrassDensityProperty = DependencyProperty.Register(nameof(SnowMountainGrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.15f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowMountainGrassDensity
        {
            get { return (float)GetValue(SnowMountainGrassDensityProperty); }
            set { SetValue(SnowMountainGrassDensityProperty, value); }
        }

        public static readonly DependencyProperty SnowGrassDensityProperty = DependencyProperty.Register(nameof(SnowGrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.25f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowGrassDensity
        {
            get { return (float)GetValue(SnowGrassDensityProperty); }
            set { SetValue(SnowGrassDensityProperty, value); }
        }

        public static readonly DependencyProperty UnderwaterObjectsDensityProperty = DependencyProperty.Register(nameof(UnderwaterObjectsDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.5f));
        [DataMember]
        [AggregateIniValueEntry]
        public float UnderwaterObjectsDensity
        {
            get { return (float)GetValue(UnderwaterObjectsDensityProperty); }
            set { SetValue(UnderwaterObjectsDensityProperty, value); }
        }

        public static readonly DependencyProperty SnowMountainsTreeDensityProperty = DependencyProperty.Register(nameof(SnowMountainsTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.01f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowMountainsTreeDensity
        {
            get { return (float)GetValue(SnowMountainsTreeDensityProperty); }
            set { SetValue(SnowMountainsTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty TreeDensityProperty = DependencyProperty.Register(nameof(TreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.003f));
        [DataMember]
        [AggregateIniValueEntry]
        public float TreeDensity
        {
            get { return (float)GetValue(TreeDensityProperty); }
            set { SetValue(TreeDensityProperty, value); }
        }

        public static readonly DependencyProperty JungleTreeDensityProperty = DependencyProperty.Register(nameof(JungleTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.66f));
        [DataMember]
        [AggregateIniValueEntry]
        public float JungleTreeDensity
        {
            get { return (float)GetValue(JungleTreeDensityProperty); }
            set { SetValue(JungleTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty RedWoodTreeDensityProperty = DependencyProperty.Register(nameof(RedWoodTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.35f));
        [DataMember]
        [AggregateIniValueEntry]
        public float RedWoodTreeDensity
        {
            get { return (float)GetValue(RedWoodTreeDensityProperty); }
            set { SetValue(RedWoodTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty SnowTreeDensityProperty = DependencyProperty.Register(nameof(SnowTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowTreeDensity
        {
            get { return (float)GetValue(SnowTreeDensityProperty); }
            set { SetValue(SnowTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty RedwoodGrassDensityProperty = DependencyProperty.Register(nameof(RedwoodGrassDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.1f));
        [DataMember]
        [AggregateIniValueEntry]
        public float RedwoodGrassDensity
        {
            get { return (float)GetValue(RedwoodGrassDensityProperty); }
            set { SetValue(RedwoodGrassDensityProperty, value); }
        }

        public static readonly DependencyProperty ShoreTreeDensityProperty = DependencyProperty.Register(nameof(ShoreTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.05f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ShoreTreeDensity
        {
            get { return (float)GetValue(ShoreTreeDensityProperty); }
            set { SetValue(ShoreTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty SnowShoreTreeDensityProperty = DependencyProperty.Register(nameof(SnowShoreTreeDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.025f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SnowShoreTreeDensity
        {
            get { return (float)GetValue(SnowShoreTreeDensityProperty); }
            set { SetValue(SnowShoreTreeDensityProperty, value); }
        }

        public static readonly DependencyProperty DeepWaterBiomesDepthProperty = DependencyProperty.Register(nameof(DeepWaterBiomesDepth), typeof(float), typeof(PGMTerrain), new PropertyMetadata(-0.24f));
        [DataMember]
        [AggregateIniValueEntry]
        public float DeepWaterBiomesDepth
        {
            get { return (float)GetValue(DeepWaterBiomesDepthProperty); }
            set { SetValue(DeepWaterBiomesDepthProperty, value); }
        }

        public static readonly DependencyProperty InlandWaterObjectsDensityProperty = DependencyProperty.Register(nameof(InlandWaterObjectsDensity), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.5f));
        [DataMember]
        [AggregateIniValueEntry]
        public float InlandWaterObjectsDensity
        {
            get { return (float)GetValue(InlandWaterObjectsDensityProperty); }
            set { SetValue(InlandWaterObjectsDensityProperty, value); }
        }

        public static readonly DependencyProperty ShorelineStartOffsetProperty = DependencyProperty.Register(nameof(ShorelineStartOffset), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.01f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ShorelineStartOffset
        {
            get { return (float)GetValue(ShorelineStartOffsetProperty); }
            set { SetValue(ShorelineStartOffsetProperty, value); }
        }

        public static readonly DependencyProperty ShorelineThicknessProperty = DependencyProperty.Register(nameof(ShorelineThickness), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.0015f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ShorelineThickness
        {
            get { return (float)GetValue(ShorelineThicknessProperty); }
            set { SetValue(ShorelineThicknessProperty, value); }
        }

        public static readonly DependencyProperty TreesGroundSlopeAccuracyProperty = DependencyProperty.Register(nameof(TreesGroundSlopeAccuracy), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.5f));
        [DataMember]
        [AggregateIniValueEntry]
        public float TreesGroundSlopeAccuracy
        {
            get { return (float)GetValue(TreesGroundSlopeAccuracyProperty); }
            set { SetValue(TreesGroundSlopeAccuracyProperty, value); }
        }

        public static readonly DependencyProperty ErosionStepsProperty = DependencyProperty.Register(nameof(ErosionSteps), typeof(int), typeof(PGMTerrain), new PropertyMetadata(4));
        [DataMember]
        [AggregateIniValueEntry]
        public int ErosionSteps
        {
            get { return (int)GetValue(ErosionStepsProperty); }
            set { SetValue(ErosionStepsProperty, value); }
        }

        public static readonly DependencyProperty ErosionStrengthProperty = DependencyProperty.Register(nameof(ErosionStrength), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.75f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ErosionStrength
        {
            get { return (float)GetValue(ErosionStrengthProperty); }
            set { SetValue(ErosionStrengthProperty, value); }
        }

        public static readonly DependencyProperty DepositionStrengthProperty = DependencyProperty.Register(nameof(DepositionStrength), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.5f));
        [DataMember]
        [AggregateIniValueEntry]
        public float DepositionStrength
        {
            get { return (float)GetValue(DepositionStrengthProperty); }
            set { SetValue(DepositionStrengthProperty, value); }
        }

        public static readonly DependencyProperty MountainGeneralTreesPercentProperty = DependencyProperty.Register(nameof(MountainGeneralTreesPercent), typeof(float), typeof(PGMTerrain), new PropertyMetadata(0.1f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MountainGeneralTreesPercent
        {
            get { return (float)GetValue(MountainGeneralTreesPercentProperty); }
            set { SetValue(MountainGeneralTreesPercentProperty, value); }
        }

        public static readonly DependencyProperty SnowBiomeLocationProperty = DependencyProperty.Register(nameof(SnowBiomeLocation), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SnowBiomeLocation
        {
            get { return (PGMTerrainXY)GetValue(SnowBiomeLocationProperty); }
            set { SetValue(SnowBiomeLocationProperty, value); }
        }

        public static readonly DependencyProperty RedWoodForestBiomeLocationProperty = DependencyProperty.Register(nameof(RedWoodForestBiomeLocation), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(Key = "RWForestBiomeLocation")]
        public PGMTerrainXY RedWoodForestBiomeLocation
        {
            get { return (PGMTerrainXY)GetValue(RedWoodForestBiomeLocationProperty); }
            set { SetValue(RedWoodForestBiomeLocationProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion1StartProperty = DependencyProperty.Register(nameof(NorthRegion1Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion1Start
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion1StartProperty); }
            set { SetValue(NorthRegion1StartProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion1EndProperty = DependencyProperty.Register(nameof(NorthRegion1End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion1End
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion1EndProperty); }
            set { SetValue(NorthRegion1EndProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion2StartProperty = DependencyProperty.Register(nameof(NorthRegion2Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion2Start
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion2StartProperty); }
            set { SetValue(NorthRegion2StartProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion2EndProperty = DependencyProperty.Register(nameof(NorthRegion2End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion2End
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion2EndProperty); }
            set { SetValue(NorthRegion2EndProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion3StartProperty = DependencyProperty.Register(nameof(NorthRegion3Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion3Start
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion3StartProperty); }
            set { SetValue(NorthRegion3StartProperty, value); }
        }

        public static readonly DependencyProperty NorthRegion3EndProperty = DependencyProperty.Register(nameof(NorthRegion3End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY NorthRegion3End
        {
            get { return (PGMTerrainXY)GetValue(NorthRegion3EndProperty); }
            set { SetValue(NorthRegion3EndProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion1StartProperty = DependencyProperty.Register(nameof(SouthRegion1Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion1Start
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion1StartProperty); }
            set { SetValue(SouthRegion1StartProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion1EndProperty = DependencyProperty.Register(nameof(SouthRegion1End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion1End
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion1EndProperty); }
            set { SetValue(SouthRegion1EndProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion2StartProperty = DependencyProperty.Register(nameof(SouthRegion2Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion2Start
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion2StartProperty); }
            set { SetValue(SouthRegion2StartProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion2EndProperty = DependencyProperty.Register(nameof(SouthRegion2End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion2End
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion2EndProperty); }
            set { SetValue(SouthRegion2EndProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion3StartProperty = DependencyProperty.Register(nameof(SouthRegion3Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion3Start
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion3StartProperty); }
            set { SetValue(SouthRegion3StartProperty, value); }
        }

        public static readonly DependencyProperty SouthRegion3EndProperty = DependencyProperty.Register(nameof(SouthRegion3End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY SouthRegion3End
        {
            get { return (PGMTerrainXY)GetValue(SouthRegion3EndProperty); }
            set { SetValue(SouthRegion3EndProperty, value); }
        }

        public static readonly DependencyProperty EastRegion1StartProperty = DependencyProperty.Register(nameof(EastRegion1Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion1Start
        {
            get { return (PGMTerrainXY)GetValue(EastRegion1StartProperty); }
            set { SetValue(EastRegion1StartProperty, value); }
        }

        public static readonly DependencyProperty EastRegion1EndProperty = DependencyProperty.Register(nameof(EastRegion1End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion1End
        {
            get { return (PGMTerrainXY)GetValue(EastRegion1EndProperty); }
            set { SetValue(EastRegion1EndProperty, value); }
        }

        public static readonly DependencyProperty EastRegion2StartProperty = DependencyProperty.Register(nameof(EastRegion2Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion2Start
        {
            get { return (PGMTerrainXY)GetValue(EastRegion2StartProperty); }
            set { SetValue(EastRegion2StartProperty, value); }
        }

        public static readonly DependencyProperty EastRegion2EndProperty = DependencyProperty.Register(nameof(EastRegion2End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion2End
        {
            get { return (PGMTerrainXY)GetValue(EastRegion2EndProperty); }
            set { SetValue(EastRegion2EndProperty, value); }
        }

        public static readonly DependencyProperty EastRegion3StartProperty = DependencyProperty.Register(nameof(EastRegion3Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion3Start
        {
            get { return (PGMTerrainXY)GetValue(EastRegion3StartProperty); }
            set { SetValue(EastRegion3StartProperty, value); }
        }

        public static readonly DependencyProperty EastRegion3EndProperty = DependencyProperty.Register(nameof(EastRegion3End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY EastRegion3End
        {
            get { return (PGMTerrainXY)GetValue(EastRegion3EndProperty); }
            set { SetValue(EastRegion3EndProperty, value); }
        }

        public static readonly DependencyProperty WestRegion1StartProperty = DependencyProperty.Register(nameof(WestRegion1Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion1Start
        {
            get { return (PGMTerrainXY)GetValue(WestRegion1StartProperty); }
            set { SetValue(WestRegion1StartProperty, value); }
        }

        public static readonly DependencyProperty WestRegion1EndProperty = DependencyProperty.Register(nameof(WestRegion1End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion1End
        {
            get { return (PGMTerrainXY)GetValue(WestRegion1EndProperty); }
            set { SetValue(WestRegion1EndProperty, value); }
        }

        public static readonly DependencyProperty WestRegion2StartProperty = DependencyProperty.Register(nameof(WestRegion2Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion2Start
        {
            get { return (PGMTerrainXY)GetValue(WestRegion2StartProperty); }
            set { SetValue(WestRegion2StartProperty, value); }
        }

        public static readonly DependencyProperty WestRegion2EndProperty = DependencyProperty.Register(nameof(WestRegion2End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion2End
        {
            get { return (PGMTerrainXY)GetValue(WestRegion2EndProperty); }
            set { SetValue(WestRegion2EndProperty, value); }
        }

        public static readonly DependencyProperty WestRegion3StartProperty = DependencyProperty.Register(nameof(WestRegion3Start), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion3Start
        {
            get { return (PGMTerrainXY)GetValue(WestRegion3StartProperty); }
            set { SetValue(WestRegion3StartProperty, value); }
        }

        public static readonly DependencyProperty WestRegion3EndProperty = DependencyProperty.Register(nameof(WestRegion3End), typeof(PGMTerrainXY), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXY WestRegion3End
        {
            get { return (PGMTerrainXY)GetValue(WestRegion3EndProperty); }
            set { SetValue(WestRegion3EndProperty, value); }
        }

        public static readonly DependencyProperty TerrainScaleMultiplierProperty = DependencyProperty.Register(nameof(TerrainScaleMultiplier), typeof(PGMTerrainXYZ), typeof(PGMTerrain), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry]
        public PGMTerrainXYZ TerrainScaleMultiplier
        {
            get { return (PGMTerrainXYZ)GetValue(TerrainScaleMultiplierProperty); }
            set { SetValue(TerrainScaleMultiplierProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            value = value.Trim('(', ')', ' ');
            var pairs = value.Split(DELIMITER);

            foreach (var pair in pairs)
            {
                var kvPair = pair.Split(new[] { '=' }, 2);
                if (kvPair.Length != 2)
                    continue;

                var key = kvPair[0].Trim();
                var val = kvPair[1].Trim();
                var propInfo = this.Properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                if (propInfo != null)
                    StringUtils.SetPropertyValue(val, this, propInfo);
                else
                {
                    propInfo = this.Properties.FirstOrDefault(f => f.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().Any(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)));
                    if (propInfo != null)
                        StringUtils.SetPropertyValue(val, this, propInfo);
                }
            }
        }

        public override string ToINIValue()
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                var val = prop.GetValue(this);
                var propValue = StringUtils.GetPropertyValue(val, prop);

                result.Append($"{propName}={propValue}");

                delimiter = DELIMITER.ToString();
            }

            return result.ToString();
        }
    }

    [DataContract]
    public class PGMTerrainXY : AggregateIniValue
    {
        public PGMTerrainXY()
        {
        }
        public PGMTerrainXY(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(float), typeof(PGMTerrainXY), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float X
        {
            get { return (float)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register(nameof(Y), typeof(float), typeof(PGMTerrainXY), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float Y
        {
            get { return (float)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            value = value.Trim('(', ')', ' ');
            var pairs = value.Split(DELIMITER);

            foreach (var pair in pairs)
            {
                var kvPair = pair.Split(new[] { '=' }, 2);
                if (kvPair.Length != 2)
                    continue;

                var key = kvPair[0].Trim();
                var val = kvPair[1].Trim();
                var propInfo = this.Properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                if (propInfo != null)
                    StringUtils.SetPropertyValue(val, this, propInfo);
                else
                {
                    propInfo = this.Properties.FirstOrDefault(f => f.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().Any(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)));
                    if (propInfo != null)
                        StringUtils.SetPropertyValue(val, this, propInfo);
                }
            }
        }

        public override string ToINIValue()
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            result.Append("(");

            var delimiter = "";
            foreach (var prop in this.Properties.OrderBy(p => p.Name))
            {
                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                var val = prop.GetValue(this);
                var propValue = StringUtils.GetPropertyValue(val, prop);

                result.Append($"{propName}={propValue}");

                delimiter = DELIMITER.ToString();
            }

            result.Append(")");
            return result.ToString();
        }
    }

    [DataContract]
    public class PGMTerrainXYZ : PGMTerrainXY
    {
        public PGMTerrainXYZ()
        {
        }
        public PGMTerrainXYZ(float x, float y, float z)
            : base(x, y)
        {
            Z = z;
        }

        public static readonly DependencyProperty ZProperty = DependencyProperty.Register(nameof(Z), typeof(float), typeof(PGMTerrainXYZ), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float Z
        {
            get { return (float)GetValue(ZProperty); }
            set { SetValue(ZProperty, value); }
        }
    }
}
