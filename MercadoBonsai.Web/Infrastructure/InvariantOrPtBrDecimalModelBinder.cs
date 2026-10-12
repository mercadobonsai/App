using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace MercadoBonsai.Web.Infrastructure;

/// <summary>
/// Model Binder inteligente para campos Decimal no ASP.NET Core.
/// Suporta nativamente tanto o padrão brasileiro (ex: "69,90" ou "1.250,50")
/// quanto valores numéricos desformatados (ex: "69.90"), além de strings com "R$".
/// Evita que "69,90" seja parseado como 6990 em ambientes onde a cultura do binding é InvariantCulture.
/// </summary>
public class InvariantOrPtBrDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var rawValue = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (bindingContext.ModelType == typeof(decimal?))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        // Limpa símbolos monetários e espaços
        string cleaned = rawValue.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();

        // Tenta parsear com pt-BR (vírgula como decimal)
        var culturePtBr = new CultureInfo("pt-BR");
        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.Currency, culturePtBr, out decimal parsedVal))
        {
            bindingContext.Result = ModelBindingResult.Success(parsedVal);
            return Task.CompletedTask;
        }

        // Tenta parsear com InvariantCulture (ponto como decimal)
        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out parsedVal))
        {
            bindingContext.Result = ModelBindingResult.Success(parsedVal);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Valor numérico inválido.");
        return Task.CompletedTask;
    }
}

public class InvariantOrPtBrDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
        {
            return new InvariantOrPtBrDecimalModelBinder();
        }

        return null;
    }
}
