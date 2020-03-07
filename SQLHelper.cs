using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Formatter {
    class SQLHelper {

        static HashSet<string> keepTokens = new HashSet<string>();
        static HashSet<string> features = new HashSet<string>();

        static SQLHelper() {
            HashSet<string> fts = new HashSet<string> { "select", "update", "insert", "delete", "drop", "create", "alter" };
            HashSet<string> tks = new HashSet<string> { "from", "left", "right", "inner", "on", "where", "group", "union", "order" };
            foreach (string token in fts) {
                features.Add(token);
                keepTokens.Add(token);
            }
            foreach (string token in tks) {
                keepTokens.Add(token);
            }
        }

        static bool inSet(string token, HashSet<string> set) {
            foreach (string tk in set) {
                if (string.Compare(tk, token, true) == 0) {
                    return true;
                }
            }
            return false;
        }

        public static bool isSQL(string text) {
            text = text.Replace("(", " ( ").Replace(")", " ) ").Replace(",", " , ").Replace("\r\n", " ").Replace("\n", " ");
            var tokens = text.Split(' ');
            if (tokens.Length > 1) {
                string first = tokens[0];
                foreach (string ft in features) {
                    if (inSet(first, features)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string format(string sql) {
            sql = sql.Replace("(", " ( ").Replace(")", " ) ").Replace(",", " , ").Replace("\r\n", " ").Replace("\n", " ");
            sql = Regex.Replace(sql, "\\s+", " ");
            var tokens = sql.Split(' ');
            StringWriter writer = new StringWriter();
            int level = 1, line = 0;
            bool preSpace = true;
            foreach (string token in tokens) {
                if (inSet(token, keepTokens)) {
                    if (line != 0) {
                        writer.Write("\r\n");
                        line++;
                    } else {
                        line++;
                    }
                    for (int i = 1; i < level; i++) {
                        writer.Write("    ");
                    }
                    writer.Write(token);
                } else {
                    if (token == "(") {
                        level++;
                        writer.Write(" ");
                        writer.Write(token);
                        preSpace = false;
                    } else if (token == ")") {
                        level--;
                        writer.Write(token);
                        preSpace = true;
                    } else if (token == ",") {
                        writer.Write(token);
                        preSpace = true;
                    } else {
                        if (preSpace) {
                            writer.Write(" ");
                        }
                        writer.Write(token);
                        preSpace = true;
                    }
                }
            }
            return writer.ToString();
        }
    }
}
