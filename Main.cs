using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Clipboard = System.Windows.Clipboard;
using Newtonsoft.Json;
using System.Windows.Controls;
using Wox.Plugin;
using System.Text;

namespace Formatter {
    public class Main : IPlugin {
        private PluginInitContext _context;
        Dictionary<string, Command> commands = new Dictionary<string, Command> {
            { "rmn", new Command("rmn", "删除换行") },
            { "adq", new Command("adq", "添加引号") },
            { "view", new Command("view", "显示剪切板文本","browser.png") } };

        readonly static string CodeJsFile = "view/code/code.js";
        readonly static string CodeHtmlFile = "view/code/code.html";

        string pluginDir = null;
        public void Init(PluginInitContext context) {
            _context = context;
            pluginDir = context.CurrentPluginMetadata.PluginDirectory;
        }

        readonly static string clipBoardScript = "clipboard.exe";
        readonly static string clipBoardData = "clipboard.txt";
        public string getClipBoard() {
            Process scriptProc = new Process();
            scriptProc.StartInfo.FileName = Path.Combine(pluginDir, clipBoardScript);
            scriptProc.StartInfo.WorkingDirectory = pluginDir;//<---very important 
            scriptProc.StartInfo.Arguments = Path.Combine(pluginDir, clipBoardData);
            scriptProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//prevent console window from popping up
            scriptProc.Start();
            scriptProc.WaitForExit();//<-- Optional if you want program running until your script exit
            scriptProc.Close();
            return File.ReadAllText(Path.Combine(pluginDir, clipBoardData), Encoding.GetEncoding("gbk"));
        }

        public string tobs64(string text) {
            return text.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
        }

        public string abbr(string text) {
            if (text != null) {
                if (text.Length > 80) {
                    text = text.Substring(0, 80) + " ......";
                }
                return text.Replace("\r\n", "\\n");
            }
            return null;
        }


        public List<Result> Query(Query query) {
            List<Result> results = new List<Result>();
            string text = getClipBoard();
            if (string.IsNullOrEmpty(text)) {
                results.Add(new Result() {
                    Title = "剪切板为空",
                    IcoPath = "Images\\copy.png"
                });
                return results;
            }
            //文本非空

            if (string.IsNullOrWhiteSpace(query.Search)) {//未使用命令
                text = text.Trim();
                if (!string.IsNullOrEmpty(text)) {
                    if (isJSON(text)) {
                        List<Result> jsonResult = handleJSON(text);
                        if (jsonResult != null) {
                            return jsonResult;
                        }
                    } else if (isXML(text)) {
                        List<Result> xmlResult = handleXML(text);
                        if (xmlResult != null) {
                            return xmlResult;
                        }
                    } else if (SQLHelper.isSQL(text)) {
                        List<Result> sqlResult = handleSQL(text);
                        if (sqlResult != null) {
                            return sqlResult;
                        }
                    }
                }
                //当成普通文本处理
                foreach (var cmd in commands) {
                    results.Add(new Result() {
                        Title = cmd.Key,
                        SubTitle = cmd.Value.desc + "：" + abbr(text),
                        IcoPath = "Images\\cmd.png",
                        Action = e => {
                            _context.API.ChangeQuery(query.ActionKeyword + " " + cmd.Key);
                            return false;
                        }
                    });
                }
            } else {
                string fmt = null, cmddesc = null;
                if (query.Search.Trim() == "rmn") {
                    fmt = Command.removeNewLine(text);
                    cmddesc = commands["rmn"].desc;
                } else if (query.Search.Trim() == "adq") {
                    fmt = Command.addQuote(text);
                    cmddesc = commands["adq"].desc;
                } else if (query.Search.Trim() == "view") {
                    Result browserItem = new Result() {
                        Title = "在代码视图中打开",
                        SubTitle = abbr(text),
                        IcoPath = "Images\\browser.png",
                        Action = e => {
                            File.WriteAllText(Path.Combine(pluginDir, CodeJsFile), "var textData=`" + tobs64(text) + "`");
                            Process.Start(Path.Combine(pluginDir, CodeHtmlFile));
                            return true;
                        }
                    };
                    Result jsonItem = new Result() {
                        Title = "在JSON视图中打开",
                        SubTitle = abbr(text),
                        IcoPath = "Images\\browser.png",
                        Action = e => {
                            File.WriteAllText(Path.Combine(pluginDir, JSONJsFile), "var jsonData=`" + tobs64(text) + "`");
                            Process.Start(Path.Combine(pluginDir, JSONHtmlFile));
                            return true;
                        }
                    };
                    Result xmlItem = new Result() {
                        Title = "在XML视图中打开",
                        SubTitle = abbr(text),
                        IcoPath = "Images\\browser.png",
                        Action = e => {
                            File.WriteAllText(Path.Combine(pluginDir, XMLJsFile), "var xmlData=`" + tobs64(text) + "`");
                            Process.Start(Path.Combine(pluginDir, XMLHtmlFile));
                            return true;
                        }
                    };
                    Result textItem = new Result() {
                        SubTitle = text,
                        IcoPath = "Images\\copy.png"
                    };
                    results.Add(browserItem);
                    results.Add(jsonItem);
                    results.Add(xmlItem);
                    results.Add(textItem);
                } else {
                    foreach (var cmd in commands) {
                        results.Add(new Result() {
                            Title = cmd.Key,
                            SubTitle = cmd.Value.desc+"："+abbr(text),
                            IcoPath = "Images\\cmd.png",
                            Action = e => {
                                _context.API.ChangeQuery(query.ActionKeyword + " " + cmd.Key);
                                return false;
                            }
                        });
                    }
                }
                if (fmt != null) {
                    results.Add(new Result() {
                        Title = "复制" + cmddesc + "后的文本",
                        SubTitle = abbr(fmt),
                        IcoPath = "Images\\copy.png",
                        Action = e => {
                            Clipboard.SetDataObject(fmt);
                            return true;
                        }
                    });
                    Result browserItem = new Result() {
                        Title = "在浏览器中查看" + cmddesc + "后的文本",
                        SubTitle = abbr(fmt),
                        IcoPath = "Images\\browser.png",
                        Action = e => {
                            File.WriteAllText(Path.Combine(pluginDir, CodeJsFile), "var textData=`" + tobs64(fmt) + "`");
                            Process.Start(Path.Combine(pluginDir, CodeHtmlFile));
                            return true;
                        }
                    };
                    results.Add(browserItem);
                }
            }
            return results;
        }
        //这边传入的参数都会保证，1：不为空，2：不为空字符串，3：去掉头尾的空格
        bool isJSON(string text) {
            if (text.StartsWith("{") || text.StartsWith("[")) {
                return true;
            }
            return false;
        }

        bool isXML(string text) {
            if (text.StartsWith("<")) {
                return true;
            }
            return false;
        }


        readonly static string JSONJsFile = "view/json/json.js";
        readonly static string JSONHtmlFile = "view/json/json.html";
        List<Result> handleJSON(string text) {
            JsonSerializer serializer = new JsonSerializer();
            object obj = null;
            try {
                obj = serializer.Deserialize(new JsonTextReader(new StringReader(text)));
            } catch { }
            if (obj != null) {

                string fmt = null, zip = null;
                {
                    StringWriter textWriter = new StringWriter();
                    JsonTextWriter jsonWriter = new JsonTextWriter(textWriter) {
                        Formatting = Formatting.Indented,
                        Indentation = 4,
                        IndentChar = ' '
                    };
                    serializer.Serialize(jsonWriter, obj);
                    fmt = textWriter.ToString();
                }

                {
                    zip = JsonConvert.SerializeObject(obj);
                }

                Result fmtItem = new Result() {
                    Title = "复制格式化后的JSON文本",
                    SubTitle = abbr(fmt),
                    IcoPath = "Images\\format.png",
                    Action = e => {
                        Clipboard.SetDataObject(fmt);
                        return true;
                    }
                };

                Result zipItem = new Result() {
                    Title = "复制压缩后的JSON文本",
                    SubTitle = abbr(zip),
                    IcoPath = "Images\\zip.png",
                    Action = e => {
                        Clipboard.SetDataObject(zip);
                        return true;
                    }
                };

                Result viewItem = new Result() {
                    Title = "在浏览器中查看JSON结构",
                    IcoPath = "Images\\browser.png",
                    Action = e => {
                        File.WriteAllText(Path.Combine(pluginDir, JSONJsFile), "var jsonData=`" + tobs64(fmt) + "`");
                        Process.Start(Path.Combine(pluginDir, JSONHtmlFile));
                        return true;
                    }
                };
                return new List<Result> { fmtItem, zipItem, viewItem };
            } else {
                return null;
            }
        }

        readonly static string XMLJsFile = "view/xml/xml.js";
        readonly static string XMLHtmlFile = "view/xml/xml.html";
        List<Result> handleXML(string text) {
            try {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(text);
                string fmt = null, zip = null;
                {
                    StringWriter sw = new System.IO.StringWriter();
                    using (System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(sw)) {
                        writer.Indentation = 4;  // the Indentation
                        writer.Formatting = System.Xml.Formatting.Indented;
                        doc.WriteContentTo(writer);
                        writer.Close();
                    }
                    fmt = sw.ToString();
                }

                {
                    StringWriter sw = new StringWriter();
                    using (System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(sw)) {
                        doc.WriteContentTo(writer);
                        writer.Close();
                    }
                    zip = sw.ToString();
                }

                Result fmtItem = new Result() {
                    Title = "复制格式化后的XML文本",
                    SubTitle = abbr(fmt),
                    IcoPath = "Images\\format.png",
                    Action = e => {
                        Clipboard.SetDataObject(fmt);
                        return true;
                    }
                };

                Result zipItem = new Result() {
                    Title = "复制压缩后的XML文本",
                    SubTitle = abbr(zip),
                    IcoPath = "Images\\zip.png",
                    Action = e => {
                        Clipboard.SetDataObject(zip);
                        return true;
                    }
                };

                Result viewItem = new Result() {
                    Title = "在浏览器中查看XML结构",
                    IcoPath = "Images\\browser.png",
                    Action = e => {
                        File.WriteAllText(Path.Combine(pluginDir, XMLJsFile), "var xmlData=`" + tobs64(fmt) + "`");
                        Process.Start(Path.Combine(pluginDir, XMLHtmlFile));
                        return true;
                    }
                };
                return new List<Result> { fmtItem, zipItem, viewItem };
            } catch {
                return null;
            }
        }

        List<Result> handleSQL(string text) {
            try {
                string fmt = SQLHelper.format(text), zip = Command.removeNewLine(text);
                Result fmtItem = new Result() {
                    Title = "复制格式化后的SQL",
                    SubTitle = abbr(fmt),
                    IcoPath = "Images\\format.png",
                    Action = e => {
                        Clipboard.SetDataObject(fmt);
                        return true;
                    }
                };

                Result zipItem = new Result() {
                    Title = "复制压缩后的SQL",
                    SubTitle = abbr(zip),
                    IcoPath = "Images\\zip.png",
                    Action = e => {
                        Clipboard.SetDataObject(zip);
                        return true;
                    }
                };

                Result viewItem = new Result() {
                    Title = "在浏览器中查看SQL",
                    IcoPath = "Images\\browser.png",
                    Action = e => {
                        File.WriteAllText(Path.Combine(pluginDir, CodeJsFile), "var inputLan='sql',textData=`" + tobs64(fmt) + "`");
                        Process.Start(Path.Combine(pluginDir, CodeHtmlFile));
                        return true;
                    }
                };
                return new List<Result> { fmtItem, zipItem, viewItem };
            } catch {
                return null;
            }
        }
    }
}
