using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Formatter {

    class Command {
        public string key { get; set; }
        public string desc { get; set; }

        public string icon { get; set; }

        public Command(string key, string desc,string icon="edit.png") {
            this.key = key;
            this.desc = desc;
            this.icon = icon;
        }

        static public string removeNewLine(string text,string rep=" ") {
            return text.Replace("\r\n",rep);
        }
        static public string addQuote(string text) { 
            return "\""+ text.Replace("\r\n", "\\n\"+\r\n\"") + "\"";
        }
    }
}
