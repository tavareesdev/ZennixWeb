// Função para atualizar lista de funcionários com base no setor
function atualizarFuncionarios(setorId) {
    var url = `/Home/GetFuncionariosBySetor?setorId=${setorId}`;

    fetch(url)
        .then(response => response.json())
        .then(funcionarios => {
            var selectFunc = document.getElementById('funcionario');
            selectFunc.innerHTML = '<option value="">Todos</option>'; // reset

            if (funcionarios.erro) {
                alert("Erro: " + funcionarios.erro);
                return;
            }

            funcionarios.forEach(f => {
                var opt = document.createElement('option');
                opt.value = f.id || f.Id;
                opt.textContent = f.nome || f.Nome;
                selectFunc.appendChild(opt);
            });

            // Após atualizar funcionários, atualiza gráfico automaticamente
            atualizarGrafico();
        })
        .catch(err => console.error("Erro ao buscar funcionários", err));
}

// Função para atualizar o gráfico via AJAX
function atualizarGrafico() {
    var setorId = document.getElementById('setor').value;
    var funcionarioId = document.getElementById('funcionario').value;

    var url = `/Home/GetDadosGrafico?setorId=${setorId}&funcionarioId=${funcionarioId}`;

    fetch(url)
        .then(response => response.json())
        .then(dados => {
            if (dados.erro) {
                alert("Erro: " + dados.erro);
                return;
            }

            chart.data.labels = dados.map(d => d.status || d.Status);
            chart.data.datasets[0].data = dados.map(d => d.quantidade || d.Quantidade);
            chart.update();
        })
        .catch(err => console.error("Erro ao buscar dados do gráfico", err));
}

// Dispara atualização quando mudar o setor (primeiro muda lista de funcionários, depois gráfico)
document.getElementById('setor').addEventListener('change', function () {
    var setorId = this.value;
    atualizarFuncionarios(setorId);
});

// Dispara atualização do gráfico quando mudar o funcionário
document.getElementById('funcionario').addEventListener('change', atualizarGrafico);

