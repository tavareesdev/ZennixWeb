let tabela = null;

$(document).ready(function () {
  // Inicializa DataTable (sem dados)
  tabela = $('#tabelaSolicitacoes').DataTable({
    searching: false,
    paging: true,
    destroy: true,
    order: [[1, 'desc']]
  });

  // Função para buscar chamados via AJAX
  function buscarChamados(filtros) {
    $.ajax({
      url: '/Chamados/BuscarChamados',
      type: 'GET',
      data: filtros,
      success: function (data) {
        tabela.clear(); // limpa os dados anteriores

        if (!data || data.length === 0) {
          tabela.draw();
          return;
        }

        var tipoPainel = $('#tabelaSolicitacoes').data('tipo-painel');

        data.forEach(function (chamado) {
          let linha;

          // cria a bolinha colorida com base na situação
          let situacaoHtml = '';
          if (chamado.Situacao === "No Prazo") {
              situacaoHtml = '<span style="display:inline-block; width:12px; height:12px; border-radius:50%; background-color:green;" title="No Prazo"></span>';
          } else if (chamado.Situacao === "Atencao") {
              situacaoHtml = '<span style="display:inline-block; width:12px; height:12px; border-radius:50%; background-color:gold;" title="Atenção"></span>';
          } else if (chamado.Situacao === "Atrasado") {
              situacaoHtml = '<span style="display:inline-block; width:12px; height:12px; border-radius:50%; background-color:red;" title="Atrasado"></span>';
          } else {
              situacaoHtml = chamado.Situacao;
          }

          if (tipoPainel === 'ChamadosDoTime') {
              linha = [
                  situacaoHtml,
                  chamado.Id,
                  chamado.Titulo,
                  chamado.DataInicio,
                  chamado.DataFim || "-",
                  chamado.Setor || "-",
                  chamado.Responsavel || "-",
                  chamado.SetorSoli || "-",
                  chamado.Solicitante || "-",
                  chamado.Status
              ];
          } else {
              linha = [
                  chamado.Id,
                  chamado.Titulo,
                  chamado.DataInicio,
                  chamado.DataFim || "-",
                  chamado.Responsavel || "-",
                  chamado.Setor || "-",
                  chamado.Status
              ];
          }

          tabela.row.add(linha);
        });

        tabela.draw();
      },
      error: function () {
        alert('Erro ao buscar chamados. Tente novamente.');
      }
    });
  }

  // Botão de busca manual
  $('#btnBuscar').click(function (e) {
    e.preventDefault();

    var filtros = {
      numero: $('#numero').val(),
      titulo: $('#titulo').val(),
      dataAbertura: $('#dataAbertura').val(),
      dataFim: $('#dataFim').val(),
      responsavel: $('#responsavel').val(),
      setor: $('#setor').val(),
      status: $('#status').val(),
      setorSoli: $('#setorSoli').val(),
      solicitante: $('#responsavelSoli').val()
    };

    buscarChamados(filtros);
  });

  $('.cards-container .card').click(function () {
    var cardType = $(this).attr('title'); // "No Prazo", "Atenção", "Atrasado", "Total"

    // Resetando todos os filtros
    var filtros = {
        numero: null,
        titulo: '',
        dataAbertura: null,
        dataFim: null,
        responsavel: '',
        setor: '',
        status: '',
        setorSoli: '',
        solicitante: '',
        situacao: '' // novo filtro
    };

    if (cardType !== 'Total') {
        filtros.status = 'Aberto'; 
        if (cardType === 'Atenção') {
            filtros.situacao = 'Atencao';
        } else {
            filtros.situacao = cardType;
        }
    }

    buscarChamados(filtros);
  });
});

// Para setor responsável
$('#setor').change(function () {
  var setorSelecionado = $(this).val();

  $('#responsavel').empty().append('<option value="">Selecione uma opção</option>');

  if (setorSelecionado) {
    $.ajax({
      url: '/Chamados/BuscarResponsaveisPorSetor',
      type: 'GET',
      data: { setor: setorSelecionado, tipo: 'responsavel' },
      success: function (data) {
        if (data && data.length > 0) {
          data.forEach(function (nome) {
            $('#responsavel').append('<option value="' + nome + '">' + nome + '</option>');
          });
        }
      },
      error: function () {
        alert('Erro ao carregar responsáveis.');
      }
    });
  }
});

// Para setor solicitante
$('#setorSoli').change(function () {
  var setorSelecionado = $(this).val();

  $('#responsavelSoli').empty().append('<option value="">Selecione uma opção</option>');

  if (setorSelecionado) {
    $.ajax({
      url: '/Chamados/BuscarResponsaveisPorSetor',
      type: 'GET',
      data: { setor: setorSelecionado, tipo: 'solicitante' },
      success: function (data) {
        if (data && data.length > 0) {
          data.forEach(function (nome) {
            $('#responsavelSoli').append('<option value="' + nome + '">' + nome + '</option>');
          });
        }
      },
      error: function () {
        alert('Erro ao carregar responsáveis.');
      }
    });
  }
});

$(document).ready(function () {
  // Debug: verificar estrutura das linhas
  $('.linha-chamado').each(function(index) {
    var $linha = $(this);
    var id = $linha.data('id');
    var hasClickHandler = $._data(this, 'events')?.click;
    
    console.log('Linha', index, 'ID:', id, 'Tem handler:', hasClickHandler);
    
    // Verificar se há elementos filhos que podem estar bloqueando
    var elementosBloqueadores = $linha.find('a, button, input, select, textarea');
    if (elementosBloqueadores.length > 0) {
      console.log(' - Elementos bloqueadores encontrados:', elementosBloqueadores.length);
    }
  });

  // Event delegation com prevenção de propagação
  $(document).on('click', '.linha-chamado', function (e) {
    // Se o clique foi em um elemento clicável, não faz nada
    if ($(e.target).is('a, button, input, select, textarea') || 
        $(e.target).closest('a, button, input, select, textarea').length) {
      return;
    }
    
    var chamadoId = $(this).data('id');
    var tipoPainel = $('#tabelaSolicitacoes').data('tipo-painel');
    
    window.location.href = '/Chamados/DadosChamado?id=' + chamadoId + '&tipoPainel=' + tipoPainel;
  });
});