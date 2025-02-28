using System.Collections;
using System.Collections.Generic;

namespace Casper
{ 
    public class PsiBatterie
    {
        public int id;
        public int tension;
        public int places;
        public bool regulated;
        public int[] modules;
        public string state;
        public float dist;

        public PsiBatterie(int id, int tension, int places, bool regulated, int[] modules, string state, float dist)
        {
            this.id = id;
            this.tension = tension;
            this.places = places;
            this.regulated = regulated;
            this.modules = modules;
            this.state = state;
            this.dist = dist;
        }
    }
}