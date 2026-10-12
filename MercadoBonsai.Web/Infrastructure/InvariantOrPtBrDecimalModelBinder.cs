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

        // 1. Se contém vírgula e NÃO contém ponto (ex: "570,00", "7,00", "2,30", "3,00", "1,100"):
        // A vírgula é OBRIGATORIAMENTE o separador decimal!
        if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            var culturePtBr = new CultureInfo("pt-BR");
            if (decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, culturePtBr, out decimal valVirgula))
            {
                bindingContext.Result = ModelBindingResult.Success(valVirgula);
                return Task.CompletedTask;
            }
        }

        // 2. Se contém ponto e NÃO contém vírgula (ex: "570.00", "7.00", "2.30", "1.100"):
        // Pode ser decimal do HTML5 type="number" ou JS ("570.00")
        if (cleaned.Contains('.') && !cleaned.Contains(','))
        {
            if (decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out decimal valPonto))
            {
                bindingContext.Result = ModelBindingResult.Success(valPonto);
                return Task.CompletedTask;
            }
        }

        // 3. Se contém ambos (ex: "1.250,50" padrão pt-BR ou "1,250.50" padrão americano):
        if (cleaned.Contains('.') && cleaned.Contains(','))
        {
            int idxPonto = cleaned.LastIndexOf('.');
            int idxVirgula = cleaned.LastIndexOf(',');

            if (idxVirgula > idxPonto)
            {
                // Padrão pt-BR (ex: "1.250,50") -> remove os pontos e usa vírgula como decimal
                string norm = cleaned.Replace(".", "");
                if (decimal.TryParse(norm, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, new CultureInfo("pt-BR"), out decimal val))
                {
                    bindingContext.Result = ModelBindingResult.Success(val);
                    return Task.CompletedTask;
                }
            }
            else
            {
                // Padrão US (ex: "1,250.50") -> remove as vírgulas e usa ponto como decimal
                string norm = cleaned.Replace(",", "");
                if (decimal.TryParse(norm, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal val))
                {
                    bindingContext.Result = ModelBindingResult.Success(val);
                    return Task.CompletedTask;
                }
            }
        }

        // 4. Número inteiro simples (sem ponto e sem vírgula, ex: "570")
        if (decimal.TryParse(cleaned, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal valInt))
        {
            bindingContext.Result = ModelBindingResult.Success(valInt);
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
