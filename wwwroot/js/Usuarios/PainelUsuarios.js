let tabela = new DataTable('#tabelaSolicitacoes', {
  searching: false,
  paging: true,
  destroy: true,
  language: {
    lengthMenu: 'Mostrar _MENU_ registros por página',
    zeroRecords: 'Nenhum resultado encontrado',
    info: 'Mostrando página _PAGE_ de _PAGES_',
    infoEmpty: 'Nenhum registro disponível',
    infoFiltered: '(filtrado de _MAX_ registros no total)',
    paginate: {
      next: 'Próximo',
      previous: 'Anterior'
    }
  }
});

$('#btnBuscar').click(function () {
  $.post('/Usuarios/Buscar', {
    setor: $('#setor').val(),
    nome: $('#nome').val(),
    cargo: $('#cargo').val(),
    status: $('#status').val()
  }, function (data) {
    tabela.clear();

    data.forEach(u => {
      const novaLinha = tabela.row.add([
        '',
        u.Nome,
        u.Setor,
        u.Cargo,
        u.Status
      ]).draw(false).node();

      if (novaLinha) {
        $(novaLinha).attr('data-id', u.Id).addClass('linha-usuario').css('cursor', 'pointer');
      }
    });

    tabela.draw();
  });
});

// Evento de clique para redirecionar
$('#tabelaSolicitacoes tbody').off('click').on('click', '.linha-usuario', function () {
  const id = $(this).data('id');
  window.location.href = `/Usuarios/DadosUsuario/${id}`;
});
