using LightMessage.Common.Messages;
using System;
using System.Collections.Generic;

public class OpponentInfo
{
    public uint Level { get; set; }
    public string Name { get; set; }
    public List<int> ActiveItems { get; set; }

    public OpponentInfo() { }

    public OpponentInfo(IReadOnlyList<Param> Params)
    {
        Level = (uint)Params[0].AsUInt.Value;
        Name = Params[1].AsString;

        var CustArray = Params[2].AsArray;
        ActiveItems = new List<int>();
        foreach (var P in CustArray)
            ActiveItems.Add((int)P.AsInt.Value);
    }
}
