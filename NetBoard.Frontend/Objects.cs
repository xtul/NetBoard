using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Frontend.Objects {
    public class PageData {
        public string Board { get; set; }
        public string BoardLong { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int TotalThreads { get; set; }
        public int ThreadsPerPage { get; set; }
    }

    public class BoardResponse {
        public PageData PageData { get; set; }
        public List<Post> Threads { get; set; }
    }

    public class Post {
        public int Id { get; set; }
        public string Image { get; set; }
        public string FileDetails { get; set; }
        public string Content { get; set; }
        public string Name { get; set; }
        public DateTime PostedOn { get; set; }
        public bool SpoilerImage { get; set; }
        public bool Archived { get; set; }
        public int PosterLevel { get; set; }
        public bool Sticky { get; set; }
        public DateTime LastPostDate { get; set; }
        public bool You { get; set; }
        public string Subject { get; set; }
        public List<Post> Responses { get; set; }
        public bool Hidden { get; set; }

        public async Task<bool> IsHidden(ILocalStorageService _localStorage) {
            var hiddenThreads = await _localStorage.GetItemAsync<List<int>>("hiddenThreads");
            bool isHidden = false;
            if (hiddenThreads is not null) {
                return hiddenThreads.Contains(Id);
            }

            return isHidden;
        }
    }
}
