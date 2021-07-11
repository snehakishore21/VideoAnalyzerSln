using System;
using System.Collections.Generic;
namespace VideoAnalyzer.Shared.Models
{
    public class KeywordInfoModel
    {
        public string Keyword { get; set; }
        public int Appeareances { get; set; }
    }

    public class SearchQueryDetail
    {
        public string Keyword { get; set; }
        public List<string> Appeareances { get; set; }
    }
}
