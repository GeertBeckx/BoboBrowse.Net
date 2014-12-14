namespace BoboBrowse.Net.Facets.Data
{
    using System;

    public abstract class TermNumberList<T> : TermValueList<T>
    {
        private const String DEFAULT_FORMATTING_STRING = "0000000000";

        protected TermNumberList()
            : base()
        {
            this.FormatString = DEFAULT_FORMATTING_STRING;
        }

        protected TermNumberList(string formatString)
            : base()
        {
            this.FormatString = formatString;
        }

        protected TermNumberList(string formatString, IFormatProvider formatProvider)
            : base()
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

        protected TermNumberList(int capacity, string formatString)
            : base(capacity)
        {
            this.FormatString = formatString;
        }

        protected TermNumberList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

        public virtual string FormatString { get; protected set; }

        public virtual IFormatProvider FormatProvider { get; protected set; }

        protected abstract object ParseString(string o);

        protected abstract double GetDoubleValue(int index);

        public override string Format(object o)
        {
            if (o == null)
                return null;
            if (o is string)
            {
                o = this.ParseString((string)o);
            }

            if (string.IsNullOrEmpty(this.FormatString))
            {
                return Convert.ToString(o);
            }
            else
            {
                //var formatString = "{0:" + this.FormatString + "}";

                if (this.FormatProvider == null)
                {
                    //return string.Format(formatString, Convert.ToDecimal(o));
                    return Convert.ToDecimal(o).ToString(this.FormatString, this.FormatProvider);
                }

                //return string.Format(this.FormatProvider, formatString, Convert.ToDecimal(o));
                return Convert.ToDecimal(o).ToString(this.FormatString);
            }
        }
    }
}