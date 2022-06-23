﻿using System.Collections.Generic;
using System.Text.Json;
using Koubot.Tool.Web;

namespace KouMaimai;

public class WahLapApi
{
    public static List<DxLocationRoot>? GetDxLocation()
    {
        var res = KouHttp.Create("http://wc.wahlap.net/maidx/rest/location").SendRequest(HttpMethods.GET).Body;
        return JsonSerializer.Deserialize<List<DxLocationRoot>?>(res);
    }
   
    public class DxLocationRoot
    {
        public string placeId { get; set; }
        public int machineCount { get; set; }
        public string id { get; set; }
        public string province { get; set; }
        public string arcadeName { get; set; }
        public string mall { get; set; }
        public string address { get; set; }
    }
}