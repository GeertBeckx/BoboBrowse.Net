// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Globalization;

    public class TermFloatList : TermNumberList<float>
    {
        private float[] _elements;
        public const float VALUE_MISSING = float.MinValue;

        private float Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                try
                {
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    float result;
                    if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        // If the invariant culture doesn't work, fall back to the passed in format provider
                        // if the provider is null, this will use the culture of the current thread by default.
                        result = float.Parse(s, NumberStyles.Any, this.FormatProvider);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (NumericUtil.IsPrefixCodedFloat(s))
                    {
                        throw new NotSupportedException("Lucene.Net index field must be a formatted string data type (typically padded with leading zeros). NumericField (FLOAT) is not supported.", ex);
                    }
                    throw ex;
                }
            }
        }

        public TermFloatList()
            : base()
        { }

        public TermFloatList(string formatString)
            : base(formatString)
        { }

        public TermFloatList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermFloatList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermFloatList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }


        public override void Add(string @value)
        {
            _innerList.Add(Parse(@value));
        }

        public override string Get(int index)
        {
            return this[index];
        }

        public override string this[int index]// From IList<string>
        {
            get
            {
                if (index < _innerList.Count)
                {
                    float val = _elements[index];
                    if (!string.IsNullOrEmpty(this.FormatString))
                    {
                        if (this.FormatProvider != null)
                        {
                            return val.ToString(this.FormatString, this.FormatProvider);
                        }
                        return val.ToString(this.FormatString);
                    }
                    return val.ToString();
                }
                return "";
            }
            set
            {
                throw new NotSupportedException("not supported");
            }
        }

        public virtual float GetPrimitiveValue(int index)
        {
            if (index < _elements.Length)
                return _elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            float val;
            if (o is string)
                val = Parse((string)o);
            else
                val = (float)o;
            return _innerList.BinarySearch(val);
        }

        public virtual int IndexOf(float o)
        {
            return Array.BinarySearch(_elements, o);
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = _innerList.ToArray();
            int negativeIndexCheck = 1;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Length > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType((short)0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                float tmp;
                for (int i = 0; i < (endPosition - negativeIndexCheck) / 2; i++)
                {
                    tmp = _elements[i + negativeIndexCheck];
                    _elements[i + negativeIndexCheck] = _elements[endPosition - i - 1];
                    _elements[endPosition - i - 1] = tmp;
                }
            }
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }

        public virtual bool Contains(float val)
        {
            return Array.BinarySearch(_elements, val) >= 0;
        }

        public override bool ContainsWithType(float val)
        {
            return Array.BinarySearch(_elements, val) >= 0;
        }

        public override int IndexOfWithType(float o)
        {
            return Array.BinarySearch(_elements, o);
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}