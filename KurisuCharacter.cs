using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KurisuBot
{
    public class KurisuCharacter
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string[] Aliases { get; set; }
        public KurisuAnime Anime { get; private set; }
        public int Variants { get; set; }
    }
}
