using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditTrafficExtreme
{
    public class LinkToSubmit
    {

        public string SubReddit
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }



    }

    public class LinkComparer : IEqualityComparer<LinkToSubmit>
    {
        public bool Equals(LinkToSubmit x, LinkToSubmit y)
        {
            return x.Title.Equals(y.Title) && x.SubReddit.Equals(y.SubReddit) && x.Url.Equals(y.Url);
        }

        public int GetHashCode(LinkToSubmit x)
        {
            return x.Title.GetHashCode() ^ x.Url.GetHashCode() ^ x.SubReddit.GetHashCode();
        }
    }

    
}
