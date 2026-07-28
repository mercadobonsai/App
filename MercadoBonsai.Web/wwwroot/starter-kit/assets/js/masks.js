/**
 * Mercado Bonsai - JavaScript Helper para Formatação Monetária e Máscaras
 * Padrão Brasileiro (R$ 0,00)
 */

document.addEventListener('DOMContentLoaded', function () {
    initMasks();
});

function initMasks() {
    // Aplica máscara monetária (R$ 0,00) nos elementos com a classe .mask-moeda
    document.querySelectorAll('.mask-moeda').forEach(function (input) {
        input.addEventListener('input', function (e) {
            formatarCampoMoeda(e.target);
        });

        // Formata valor inicial se existir
        if (input.value) {
            formatarCampoMoeda(input);
        }
    });

    // Submissão limpa de formulários com campos monetários
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('.mask-moeda').forEach(function (input) {
                // Se o formulário espera valor decimal C#, desformata a moeda
                let rawVal = desformatarMoeda(input.value);
                if (input.dataset.unmaskOnSubmit === "true") {
                    input.value = rawVal;
                }
            });
        });
    });
}

function formatarCampoMoeda(input) {
    let value = input.value.replace(/\D/g, '');
    if (!value) {
        input.value = '';
        return;
    }
    value = (parseInt(value, 10) / 100).toFixed(2);
    value = value.replace('.', ',');
    value = value.replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1.');
    input.value = 'R$ ' + value;
}

function desformatarMoeda(valorFormatado) {
    if (!valorFormatado) return '0.00';
    let limpo = valorFormatado.replace('R$', '').replace(/\./g, '').replace(',', '.').trim();
    return parseFloat(limpo) || 0.00;
}
