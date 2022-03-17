using System;
using MED = DbgCensus.Core.Objects.MetagameEventDefinition;

namespace UVOCBot.Plugins.Planetside.Objects;

public static class MetagameEventDefinitionToDuration
{
    public static TimeSpan GetDuration(MED definition)
        => definition switch
        {
            MED.NCMeltdownAmerish or MED.TRMeltdownAmerish or MED.VSMeltdownAmerish => TimeSpan.FromMinutes(90),
            MED.NCMeltdownEsamir or MED.TRMeltdownEsamir or MED.VSMeltdownEsamir => TimeSpan.FromMinutes(90),
            MED.NCMeltdownHossin or MED.TRMeltdownHossin or MED.VSMeltdownHossin => TimeSpan.FromMinutes(90),
            MED.NCMeltdownIndar or MED.TRMeltdownIndar or MED.VSMeltdownIndar => TimeSpan.FromMinutes(90),
            MED.NCMeltdownKoltyr or MED.TRMeltdownKoltyr or MED.VSMeltdownKoltyr => TimeSpan.FromMinutes(45),
            MED.NCMeltdownOshur or MED.TRMeltdownOshur or MED.VSMeltdownOshur => TimeSpan.FromMinutes(90),
            MED.NCUnstableMeltdownAmerish or MED.TRUnstableMeltdownAmerish or MED.VSUnstableMeltdownAmerish => TimeSpan.FromMinutes(45),
            MED.NCUnstableMeltdownEsamir or MED.TRUnstableMeltdownEsamir or MED.VSUnstableMeltdownEsamir => TimeSpan.FromMinutes(45),
            MED.NCUnstableMeltdownHossin or MED.TRUnstableMeltdownHossin or MED.VSUnstableMeltdownHossin => TimeSpan.FromMinutes(45),
            MED.NCUnstableMeltdownIndar or MED.TRUnstableMeltdownIndar or MED.VSUnstableMeltdownIndar => TimeSpan.FromMinutes(45),
            MED.ConquestAmerish or MED.ConquestEsamir or MED.ConquestHossin or MED.ConquestIndar or MED.ConquestOshur => TimeSpan.FromMinutes(90),
            MED.ConquestKoltyr => TimeSpan.FromMinutes(45),
            _ => TimeSpan.FromMinutes(90)
        };
}
