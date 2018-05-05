//=========       Copyright © Reperio Studios 2013-2018 @ Bernt Andreas Eide!       ============//
//
// Purpose: A simple material file (VMT) parsed as a basic KV file.
//
//=============================================================================================//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCleaner
{
    public class Material : IMaterial, IDisposable
    {
        protected bool _valid;
        protected string _content;
        protected string _shader;
        protected Dictionary<string, string> _data;
        public Material(string fileOrContent)
        {
            _valid = false;
            _content = null;

            if (File.Exists(fileOrContent))
                _content = File.ReadAllText(fileOrContent);
            else
                _content = fileOrContent;

            if (string.IsNullOrEmpty(_content))
                return;

            _data = new Dictionary<string, string>();
            ParseMaterial();
        }

        ~Material()
        {
            Dispose(false);
        }

        public bool IsMaterialValid()
        {
            return _valid;
        }

        public string getShader()
        {
            return _shader;
        }

        public string getParam(string key)
        {
            if (_data.ContainsKey(key))
                return _data[key];

            return null;
        }

        public string getBaseTexture1()
        {
            return getParam("$basetexture");
        }

        public string getBaseTexture2()
        {
            return getParam("$basetexture2");
        }

        public string surfaceprop()
        {
            return getParam("surfaceprop");
        }

        public string getBumpMap()
        {
            string val = getParam("bumpmap");
            if (string.IsNullOrEmpty(val))
                return getParam("normalmap");

            return val;
        }

        public string[] getProxies()
        {
            return null; // TODO
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool bSafe)
        {
            _content = null;
            _data = null;
        }

        protected void CleanMaterial()
        {
            StringBuilder b = new StringBuilder();
            using (StringReader r = new StringReader(_content))
            {
                while (r.Peek() > -1)
                {
                    string line = r.ReadLine();
                    string line_nowhitespace = line.Replace(" ", "").Replace("\t", "");
                    if (string.IsNullOrEmpty(line_nowhitespace) || string.IsNullOrWhiteSpace(line_nowhitespace) || line_nowhitespace.StartsWith("//"))
                        continue;

                    // If the line has a comment, remove it:
                    int indexOfComment = line.LastIndexOf("//");
                    if (indexOfComment >= 0)
                        line = line.Substring(0, (indexOfComment - 1));

                    b.AppendLine(line);
                }
            }
            _content = b.ToString();
            b = null;
        }

        protected string GetLine(StringReader r)
        {
            string line = r.ReadLine();
            if (line.Contains("\""))
            {
                int numQuotes = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == '"')
                        numQuotes++;
                }

                if ((numQuotes % 2) != 0) // Invalid!                                    
                    return null;
            }

            return line.Replace("\"", "");
        }

        protected bool IsCharMatchingChar(char a, char b, bool equal)
        {
            if ((equal && (a == b)) ||
                (!equal && (a != b)))
                return true;

            return false;
        }

        protected int FindIndexOfPieceInString(string line, int start, bool equal, char sym, bool rev = false)
        {
            int size = line.Length;
            if (rev)
            {
                for (int i = (size - 1); i >= start; i--)
                {
                    if (IsCharMatchingChar(line[i], sym, equal))
                        return i;
                }
            }
            else
            {
                for (int i = start; i < size; i++)
                {
                    if (IsCharMatchingChar(line[i], sym, equal))
                        return i;
                }
            }

            return -1;
        }

        protected bool AddKVPair(string line)
        {
            try
            {
                int firstNonWhitespace = FindIndexOfPieceInString(line, 0, false, ' ');
                int firstWhitespaceFromPrev = FindIndexOfPieceInString(line, (firstNonWhitespace + 1), true, ' ');

                int nextNonWhitespace = FindIndexOfPieceInString(line, (firstWhitespaceFromPrev + 1), false, ' ');
                int lastNonWhitespace = FindIndexOfPieceInString(line, 0, false, ' ', true);

                string key = line.Substring(firstNonWhitespace, (firstWhitespaceFromPrev - firstNonWhitespace));
                string value = line.Substring(nextNonWhitespace, (lastNonWhitespace - nextNonWhitespace) + 1);

                if (!_data.ContainsKey(key))
                    _data.Add(key.ToLower(), value.ToLower());

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected void ParseMaterial()
        {
            CleanMaterial(); // Remove unnecessary stuff.            

            int numLeftBrackets = 0, numRightBrackets = 0;
            for (int i = 0; i < _content.Length; i++)
            {
                if (_content[i] == '{')
                    numLeftBrackets++;

                if (_content[i] == '}')
                    numRightBrackets++;
            }

            if (numLeftBrackets != numRightBrackets) // Invalid material!                            
                return;

            List<string> internalData = new List<string>();

            using (StringReader r = new StringReader(_content))
            {
                _shader = GetLine(r);
                if (_shader == null)
                    return;

                while (r.Peek() > -1)
                {
                    string line = GetLine(r);
                    if (line == null)
                        return;

                    internalData.Add(line);
                }
            }

            int size = internalData.Count();
            int currIdx = 0;
            foreach (string line in internalData)
            {
                string lineNoWhiteSpace = line.ToLower().Replace(" ", "").Replace("\t", "");
                string nextLine = ((currIdx + 1) < size) ? internalData[currIdx + 1].ToLower().Replace(" ", "").Replace("\t", "") : null;
                if (lineNoWhiteSpace.Equals("proxies") ||
                    lineNoWhiteSpace.StartsWith("{") ||
                    lineNoWhiteSpace.StartsWith("}") ||
                    lineNoWhiteSpace.StartsWith("[") ||
                    lineNoWhiteSpace.StartsWith("]") ||
                    ((nextLine != null) && (nextLine.Contains("{")))
                    )
                {
                    // TODO: Could parse proxies and such, DXT overrides...
                    currIdx++;
                    continue;
                }

                if (!AddKVPair(line.Replace("\t", " "))) // If unable to handle line, assume it is bogus.
                    return;

                currIdx++;
            }

            _valid = true;
        }
    }
}
