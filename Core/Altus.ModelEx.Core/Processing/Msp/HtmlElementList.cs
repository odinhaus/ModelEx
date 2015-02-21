using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Dynamic;

namespace Altus.Core.Processing.Msp
{
    public class HtmlElementList : DynamicObject, IEnumerable<HtmlElement>
    {
        private string _idElement = @"(?<open><(?<tag>\w+)\s+.*?id\s*=\s*\""(?<id>\w+)\""[^\>]*>)(?<html>.*?)(?<close></\2>)";
        private Dictionary<string, HtmlElement> _innerList = new Dictionary<string, HtmlElement>();

        public HtmlElementList(string htmlSource)
        {
            ParseHtml(htmlSource);
        }

        private void ParseHtml(string htmlSource)
        {
            Regex r = new Regex(_idElement, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
            Match m = r.Match(htmlSource);
            while (m != null && m.Success)
            {
                HtmlElement elm = new HtmlElement(
                    m.Groups["tag"].Value,
                    m.Groups["id"].Value,
                    m.ToString(),
                    m.Groups["html"].Value,
                    m.Groups["open"].Value);
                _innerList.Add(elm.Id, elm);
                ParseHtml(m.Groups["html"].Value);
                m = m.NextMatch();
            }
        }

        public bool Contains(string id)
        {
            return _innerList.ContainsKey(id);
        }

        public HtmlElement this[string id]
        {
            get 
            {
                return _innerList[id];
            }
            set 
            {
                _innerList[id] = value;
            }
        }

        public IEnumerator<HtmlElement> GetEnumerator()
        {
            foreach (HtmlElement e in _innerList.Values)
                yield return e;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name;
            HtmlElement elm;
            bool found = _innerList.TryGetValue(name, out elm);
            if (found)
            {
                result = elm;
            }
            else
            {
                result = null;
            }
            return found;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            string name = binder.Name;
            HtmlElement elm;
            bool found = _innerList.ContainsKey(name);
            if (found)
            {
                _innerList[name] = (HtmlElement)value;
            }
            return found;
        }
    }

    public class HtmlElement
    {
        private string _outerHead;
        private string _outerClose;
        private string _markers = @"(?<open><(?<tag>\w+)[^\>]*>?)(?<html>.*?)(?<close></\2>)";

        public HtmlElement(string type, string id, string outerHtml, string innerHtml, string outerHtmlHeader)
        {
            this.ElementType = type;
            this.Id = id;
            this.InnerHtml = innerHtml;
            this.OuterHtmlOriginal = outerHtml;
            this.InnerHtmlOriginal = innerHtml;
            this._outerHead = outerHtmlHeader;
            this._outerClose = "</" + type + ">";
        }

        public string ElementType { get; private set; }
        public string Id { get; private set; }
        public string OuterHtmlOriginal { get; private set; }
        public string InnerHtmlOriginal { get; private set; }
        public string OuterHtml 
        { 
            get { return _outerHead + InnerHtml + _outerClose; }
            set
            {
                Regex r = new Regex(_markers, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                Match m = r.Match(value);
                if (m.Success)
                {
                    _outerHead = m.Groups["open"].Value;
                    _outerClose = "</" + m.Groups["tag"].Value + ">";
                    this.InnerHtml = value.Replace(_outerHead, "").Replace(_outerClose, "");
                }
                else
                {
                    _outerClose = "";
                    _outerHead = "";
                    this.InnerHtml = value;
                }
            }
        }
        public string InnerHtml { get; set; }
        public void Remove()
        {
            _outerHead = _outerClose = "";
            this.InnerHtml = "";
        }
    }
}
