using System;
using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Controller.LiveTv;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public class GenreMapper
    {       
        public const string GENRE_EDUCATIONAL = "GENREEDUCATIONAL";
        public const string GENRE_KIDS = "GENREKIDS";
        public const string GENRE_LIVE = "GENRELIVE";
        public const string GENRE_MOVIE = "GENREMOVIE";
        public const string GENRE_NEWS = "GENRENEWS";
        public const string GENRE_SPORT = "GENRESPORT";
        
        private readonly List<string> EducationalGenres;
        private readonly List<string> KidsGenres;
        private readonly List<string> LiveGenres;
        private readonly List<string> MovieGenres;
        private readonly List<string> NewsGenres;
        private readonly List<string> SportGenres;

        public GenreMapper(MediaPortalOptions configuration)
        {
            EducationalGenres = new List<string>();
            KidsGenres = new List<string>();
            LiveGenres = new List<string>();
            MovieGenres = new List<string>();
            NewsGenres = new List<string>();
            SportGenres = new List<string>();

            LoadInternalLists(configuration.GenreMappings);
        }

        private void LoadInternalLists(Dictionary<string, List<string>> genreMappings)
        {
            if (genreMappings != null)
            {
                if (genreMappings.ContainsKey(GENRE_EDUCATIONAL) && genreMappings[GENRE_EDUCATIONAL] != null)
                {
                    EducationalGenres.AddRange(genreMappings[GENRE_EDUCATIONAL]);
                }

                if (genreMappings.ContainsKey(GENRE_KIDS) && genreMappings[GENRE_KIDS] != null)
                {
                    KidsGenres.AddRange(genreMappings[GENRE_KIDS]);
                }

                if (genreMappings.ContainsKey(GENRE_LIVE) && genreMappings[GENRE_LIVE] != null)
                {
                    LiveGenres.AddRange(genreMappings[GENRE_LIVE]);
                }

                if (genreMappings.ContainsKey(GENRE_MOVIE) && genreMappings[GENRE_MOVIE] != null)
                {
                    MovieGenres.AddRange(genreMappings[GENRE_MOVIE]);
                }

                if (genreMappings.ContainsKey(GENRE_NEWS) && genreMappings[GENRE_NEWS] != null)
                {
                    NewsGenres.AddRange(genreMappings[GENRE_NEWS]);
                }

                if (genreMappings.ContainsKey(GENRE_SPORT) && genreMappings[GENRE_SPORT] != null)
                {
                    SportGenres.AddRange(genreMappings[GENRE_SPORT]);
                }
            }
        }

        public void SetProgramCategories(ProgramInfo program)
        {
            if (program != null)
            {
                if (program.Genres.Any() == true)
                {
                    program.IsEducational = EducationalGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase)); 
                    program.IsKids = KidsGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase));
                    program.IsLive = LiveGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase));
                    program.IsMovie = MovieGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase));
                    program.IsNews = NewsGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase));
                    program.IsSports = SportGenres.Any(g => program.Genres.Contains(g, StringComparer.InvariantCultureIgnoreCase));
                }
            }
        }
    }
}
