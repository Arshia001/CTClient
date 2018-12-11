using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotSoSimpleJSON
{
    public enum JSONNodeType
    {
        Array,
        Class,
        StringValue,
        IntValue,
        DoubleValue,
        BoolValue,
        NullValue,
        Nonexistant
    }

    public abstract class JSONNode
    {
        public bool IsArray { get { return Type == JSONNodeType.Array; } }

        public bool IsClass { get { return Type == JSONNodeType.Class; } }

        public bool IsNull { get { return Type == JSONNodeType.NullValue; } }

        public virtual JSONNode this[int aIndex] { get { return new JSONNonexistant(); } set { throw new NotSupportedException(); } }

        public virtual JSONNode this[string aKey] { get { return new JSONNonexistant(); } set { throw new NotSupportedException(); } }

        public virtual int Count { get { return -1; } }

        public virtual IEnumerable<JSONNode> Children { get { throw new NotSupportedException(); } }

        public virtual void Add(JSONNode aItem)
        {
            throw new NotSupportedException();
        }

        public virtual void Add(string aKey, JSONNode aItem)
        {
            throw new NotSupportedException();
        }

        public virtual JSONNode Remove(string aKey)
        {
            throw new NotSupportedException();
        }

        public virtual JSONNode Remove(int aIndex)
        {
            throw new NotSupportedException();
        }

        public virtual JSONNode Remove(JSONNode aNode)
        {
            throw new NotSupportedException();
        }

        public abstract string Serialize();


        public abstract JSONNodeType Type { get; }

        public virtual int? AsInt
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual double? AsDouble
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual bool? AsBool
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual JSONArray AsArray
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual JSONClass AsClass
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual string AsString
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual Dictionary<string, JSONNode> AsDictionary
        {
            get
            {
                throw new NotSupportedException();
            }
        }


        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(JSONNode N, object O)
        {
            if (O == null && (N is JSONNull || N is JSONNonexistant))
                return true;
            else if ((O is int || O is int?) && N is JSONInt)
                return N.AsInt == (int)O;
            else if ((O is float || O is float? || O is double || O is double?) && N is JSONDouble)
                return N.AsDouble == (double)O;
            else if ((O is bool || O is bool?) && N is JSONBool)
                return N.AsBool == (bool)O;
            else if (O is string && N is JSONString)
                return N.AsString == (string)O;
            else
                return ReferenceEquals(N, O);
        }

        public static bool operator !=(JSONNode N, object O)
        {
            return !(N == O);
        }

        public static implicit operator JSONNode(string s)
        {
            return new JSONString(s);
        }

        public static implicit operator JSONNode(int s)
        {
            return new JSONInt(s);
        }

        public static implicit operator JSONNode(float s)
        {
            return new JSONDouble(s);
        }

        public static implicit operator JSONNode(double s)
        {
            return new JSONDouble(s);
        }

        public static implicit operator JSONNode(bool s)
        {
            return new JSONBool(s);
        }


        internal static string Escape(string aText)
        {
            StringBuilder result = new StringBuilder(aText.Length);
            foreach (char c in aText)
            {
                switch (c)
                {
                    case '\\':
                        result.Append("\\\\");
                        break;
                    case '\"':
                        result.Append("\\\"");
                        break;
                    case '\n':
                        result.Append("\\n");
                        break;
                    case '\r':
                        result.Append("\\r");
                        break;
                    case '\t':
                        result.Append("\\t");
                        break;
                    case '\b':
                        result.Append("\\b");
                        break;
                    case '\f':
                        result.Append("\\f");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        static JSONNode Numberize(string token)
        {
            bool flag = false;
            int integer = 0;
            double real = 0;

            if (int.TryParse(token, out integer))
            {
                return new JSONInt(integer);
            }

            if (double.TryParse(token, out real))
            {
                return new JSONDouble(real);
            }

            if (bool.TryParse(token, out flag))
            {
                return new JSONBool(flag);
            }

            throw new NotImplementedException(token);
        }

        static JSONNode GetElement(string token, bool tokenIsString)
        {
            if (tokenIsString)
            {
                return new JSONString(token);
            }
            else if (token == "null")
            {
                return new JSONNull();
            }
            else
            {
                return Numberize(token);
            }
        }

        static void AddElement(JSONNode ctx, string token, string tokenName, bool tokenIsString)
        {
            if (ctx.IsArray)
                ctx.Add(GetElement(token, tokenIsString));
            else if (ctx.IsClass)
                ctx.Add(tokenName, GetElement(token, tokenIsString));
            else
                throw new NotSupportedException();
        }

        public static JSONNode Parse(string aJSON)
        {
            Stack<JSONNode> stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            int i = 0;
            string Token = "";
            string TokenName = "";
            bool QuoteMode = false;
            bool TokenIsString = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        stack.Push(new JSONClass());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();

                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName, stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token != "")
                        {
                            TokenName = TokenName.Trim();
                            /*
							if (ctx is JSONArray)
								ctx.Add (Token);
							else if (TokenName != "")
								ctx.Add (TokenName, Token);
								*/
                            AddElement(ctx, Token, TokenName, TokenIsString);
                            TokenIsString = false;
                        }
                        TokenName = "";
                        Token = "";
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        TokenName = Token;
                        Token = "";
                        TokenIsString = false;
                        break;

                    case '"':
                        QuoteMode ^= true;
                        TokenIsString = QuoteMode == true ? true : TokenIsString;
                        break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (Token != "")
                        {
                            /*
							if (ctx is JSONArray) {
								ctx.Add (Token);
							} else if (TokenName != "") {
								ctx.Add (TokenName, Token);
							}
							*/
                            AddElement(ctx, Token, TokenName, TokenIsString);
                            TokenIsString = false;

                        }
                        TokenName = "";
                        Token = "";
                        TokenIsString = false;
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token += aJSON[i];
                        break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't':
                                    Token += '\t';
                                    break;
                                case 'r':
                                    Token += '\r';
                                    break;
                                case 'n':
                                    Token += '\n';
                                    break;
                                case 'b':
                                    Token += '\b';
                                    break;
                                case 'f':
                                    Token += '\f';
                                    break;
                                case 'u':
                                    {
                                        string s = aJSON.Substring(i + 1, 4);
                                        Token += (char)int.Parse(
                                            s,
                                            System.Globalization.NumberStyles.AllowHexSpecifier);
                                        i += 4;
                                        break;
                                    }
                                default:
                                    Token += C;
                                    break;
                            }
                        }
                        break;

                    default:
                        Token += aJSON[i];
                        break;
                }
                ++i;
            }
            if (QuoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seem to be messed up.");
            }

            if (ctx == null)
                return GetElement(Token, TokenIsString);

            return ctx;
        }

        public virtual bool IsEmpty
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }

    public class JSONArray : JSONNode, IEnumerable<JSONNode>
    {
        private List<JSONNode> m_List = new List<JSONNode>();

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return new JSONNonexistant();
                return m_List[aIndex];
            }
            set
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.Array;
            }
        }

        public override JSONArray AsArray
        {
            get
            {
                return this;
            }
        }

        public override int Count
        {
            get { return m_List.Count; }
        }

        public override bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public override void Add(JSONNode aItem)
        {
            m_List.Add(aItem);
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            JSONNode tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            m_List.Remove(aNode);
            return aNode;
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                return (IEnumerable<JSONNode>)GetEnumerator();
            }
        }

        public IEnumerator<JSONNode> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        public override string Serialize()
        {
            StringBuilder result = new StringBuilder("[");
            bool bFirst = true;
            foreach (JSONNode N in m_List)
            {
                if (bFirst)
                    bFirst = false;
                else
                    result.Append(",");

                result.Append(N.Serialize());
            }
            result.Append("]");
            return result.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_List.GetEnumerator();
        }
    }

    public class JSONClass : JSONNode, IEnumerable<KeyValuePair<string, JSONNode>>
    {
        private Dictionary<string, JSONNode> m_Dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                else
                    return new JSONNonexistant();
            }
            set
            {
                m_Dict[aKey] = value;
            }
        }

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.Class;
            }
        }


        public override JSONClass AsClass
        {
            get
            {
                return this;
            }
        }

        public override Dictionary<string, JSONNode> AsDictionary
        {
            get
            {
                return new Dictionary<string, JSONNode>(m_Dict);
            }
        }

        public override int Count
        {
            get { return m_Dict.Count; }
        }

        public override bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
                m_Dict[aKey] = aItem;
            else
                throw new NotSupportedException();
        }

        public override JSONNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey))
                return null;
            JSONNode tmp = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return tmp;
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            var item = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(item.Key);
            return item.Value;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            try
            {
                var item = m_Dict.Where(k => k.Value == aNode).First();
                m_Dict.Remove(item.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (KeyValuePair<string, JSONNode> N in m_Dict)
                    yield return N.Value;
            }
        }

        public IEnumerator<KeyValuePair<string, JSONNode>> GetEnumerator()
        {
            return m_Dict.GetEnumerator();
        }

        public override string Serialize()
        {
            StringBuilder result = new StringBuilder("{");
            bool bFirst = true;
            foreach (KeyValuePair<string, JSONNode> N in m_Dict)
            {
                if (bFirst)
                    bFirst = false;
                else
                    result.Append(",");

                result.Append("\"" + Escape(N.Key) + "\":" + N.Value.Serialize());
            }
            result.Append("}");
            return result.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dict.GetEnumerator();
        }
    }

    public class JSONInt : JSONNode
    {
        int data;

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.IntValue;
            }
        }

        public override int? AsInt
        {
            get
            {
                return data;
            }
        }

        public override double? AsDouble
        {
            get
            {
                return data;
            }
        }

        public override string AsString
        {
            get
            {
                return Serialize();
            }
        }

        public JSONInt(int data)
        {
            this.data = data;
        }

        public override string Serialize()
        {
            return data.ToString();
        }
    }

    public class JSONDouble : JSONNode
    {
        double data;

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.DoubleValue;
            }
        }

        public override double? AsDouble
        {
            get
            {
                return data;
            }
        }

        public override int? AsInt
        {
            get
            {
                return (int)data;
            }
        }

        public override string AsString
        {
            get
            {
                return Serialize();
            }
        }

        public JSONDouble(double data)
        {
            this.data = data;
        }

        public override string Serialize()
        {
            return data.ToString();
        }
    }

    public class JSONBool : JSONNode
    {
        bool data;

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.BoolValue;
            }
        }

        public override bool? AsBool
        {
            get
            {
                return data;
            }
        }

        public override string AsString
        {
            get
            {
                return Serialize();
            }
        }

        public JSONBool(bool data)
        {
            this.data = data;
        }

        public override string Serialize()
        {
            return data.ToString();
        }
    }

    public class JSONString : JSONNode
    {
        string data;

        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.StringValue;
            }
        }

        public override string AsString
        {
            get
            {
                return data;
            }
        }

        public override bool? AsBool
        {
            get
            {
                bool res;
                return bool.TryParse(data, out res) ? (bool?)res : null;
            }
        }

        public override int? AsInt
        {
            get
            {
                int res;
                return int.TryParse(data, out res) ? (int?)res : null;
            }
        }

        public override double? AsDouble
        {
            get
            {
                double res;
                return double.TryParse(data, out res) ? (double?)res : null;
            }
        }

        public JSONString(string data)
        {
            this.data = data;
        }

        public override string Serialize()
        {
            return "\"" + Escape(data) + "\"";
        }
    }

    public class JSONNull : JSONNode
    {
        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.NullValue;
            }
        }

        public override string Serialize()
        {
            return "null";
        }
    }

    class JSONNonexistant : JSONNode
    {
        public override JSONNodeType Type
        {
            get
            {
                return JSONNodeType.Nonexistant;
            }
        }

        public override string Serialize()
        {
            throw new NotSupportedException();
        }

        public override JSONArray AsArray
        {
            get
            {
                return null;
            }
        }

        public override bool? AsBool
        {
            get
            {
                return null;
            }
        }

        public override JSONClass AsClass
        {
            get
            {
                return null;
            }
        }

        public override double? AsDouble
        {
            get
            {
                return null;
            }
        }

        public override int? AsInt
        {
            get
            {
                return null;
            }
        }

        public override string AsString
        {
            get
            {
                return null;
            }
        }

        public override int Count
        {
            get
            {
                return 0;
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return true;
            }
        }
    }

    public static class JSON
    {
        public static JSONNode Parse(string aJSON)
        {
            return JSONNode.Parse(aJSON);
        }

        public static JSONNode FromData(object data)
        {
            if (data == null)
                return new JSONNull();
            if (data is JSONNode)
                return (JSONNode)data;
            if (data is IJSONSerializable)
                return ((IJSONSerializable)data).ToJson();
            if (data is int || data is int?)
                return new JSONInt((int)data);
            if (data is float || data is float? || data is double || data is double?)
                return new JSONDouble((double)data);
            if (data is bool || data is bool?)
                return new JSONBool((bool)data);
            if (data is string)
                return new JSONString((string)data);
            if (data is IEnumerable)
            {
                var Result = new JSONArray();
                foreach (var Item in (IEnumerable)data)
                    Result.Add(FromData(Item));
                return Result;
            }
            if (data is IDictionary)
            {
                var Result = new JSONClass();
                var Dic = (IDictionary)data;
                foreach (var Key in Dic.Keys)
                    Result.Add(Key.ToString(), FromData(Dic[Key]));
                return Result;
            }

            throw new NotSupportedException();
        }
    }

    public interface IJSONSerializable
    {
        JSONNode ToJson();
    }
}
