using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TechSolutions.Binders
{
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == null) return null;

            var rawValue = valueResult.AttemptedValue;
            if (string.IsNullOrWhiteSpace(rawValue)) return 0m;

            rawValue = rawValue.Replace(",", ".");

            decimal result;
            if (decimal.TryParse(rawValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;

            bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                "Invalid amount.");
            return null;
        }
    }
}