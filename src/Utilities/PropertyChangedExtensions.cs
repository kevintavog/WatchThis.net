using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WatchThis.Utilities
{
    static public class PropertyChangedExtensions
    {
        static public void FirePropertyChanged<T>(this INotifyPropertyChanged notifier, PropertyChangedEventHandler propertyChanged, Expression<Func<T>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentNullException("selectorExpression");
            var body = selectorExpression.Body as MemberExpression;
            if (body == null)
                throw new ArgumentException("The body must be a member expression");

            var handler = propertyChanged;
            if (handler != null)
                handler(notifier, new PropertyChangedEventArgs(body.Member.Name));
        }

        static public bool SetField<T>(this INotifyPropertyChanged notifier, PropertyChangedEventHandler propertyChanged, ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            notifier.FirePropertyChanged(propertyChanged, selectorExpression);
            return true;
        }
    }
}
