using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace whoWasIn.Shared
{
    public class MultiResults
    {
        public IEnumerable<MovieDetails> Movies { get; set; }
        public IEnumerable<PersonDetails> People { get; set; }
        public IEnumerable<TvShowDetails> TvShows { get; set; }
    }
}