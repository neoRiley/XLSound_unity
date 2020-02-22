using System;
using System.Linq.Expressions;
using System.Reflection;



public static class PropertyHelper
{
    public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> property)
    {
        PropertyInfo propertyInfo = null;
        var body = property.Body;

        if (body is MemberExpression)
        {
            propertyInfo = (body as MemberExpression).Member as PropertyInfo;
        }
        else if (body is UnaryExpression)
        {
            propertyInfo = ((MemberExpression)((UnaryExpression)body).Operand).Member as PropertyInfo;
        }

        if (propertyInfo == null)
        {
            throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
        }

        var propertyName = propertyInfo.Name;

        return propertyName;
    }

    public static string GetPropertyName<T>(Expression<Func<T, object>> property)
    {
        PropertyInfo propertyInfo = null;
        var body = property.Body;

        if (body is MemberExpression)
        {
            propertyInfo = (body as MemberExpression).Member as PropertyInfo;
        }
        else if (body is UnaryExpression)
        {
            propertyInfo = ((MemberExpression)((UnaryExpression)body).Operand).Member as PropertyInfo;
        }

        if (propertyInfo == null)
        {
            throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
        }

        var propertyName = propertyInfo.Name;

        return propertyName;
    }

    //GETNAME(new { variable });
    public static string GetPropertyName<T>(T myInput) where T : class
    {
        if (myInput == null)
            return string.Empty;

        return typeof(T).GetProperties()[0].Name;
    }
}
