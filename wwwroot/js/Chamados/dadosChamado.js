/**
 * Alterna a exibição das seções da tela de chamado (Dados, Histórico, Anexos, Comentários)
 * @param {string} secao - Identificador da seção a ser exibida: 'dados', 'historico', 'anexos', 'comentarios'
*/

function mostrarConteudo(secao) {

  abaDados = document.getElementById('dados')
  abaHistorico = document.getElementById('Historico')
  abaAnexos = document.getElementById('Anexos')
  abaComentarios = document.getElementById('Comentarios')

  btnDados = document.getElementById('btnDados')
  btnComentarios = document.getElementById('btnComentarios')
  btnHistorico = document.getElementById('btnHistorico')
  btnAnexos = document.getElementById('btnAnexos')

  if(secao == 'dados') {
    abaDados.style.display = "block"
    abaHistorico.style.display = "none"
    abaAnexos.style.display = "none"
    abaComentarios.style.display = "none"

    btnDados.classList.remove("active")
    btnComentarios.classList.remove("active")
    btnHistorico.classList.remove("active")
    btnAnexos.classList.remove("active")

    btnDados.classList.add("active")
  } else if (secao == 'historico') {
    abaDados.style.display = "none"
    abaHistorico.style.display = "block"
    abaAnexos.style.display = "none"
    abaComentarios.style.display = "none"

    btnDados.classList.remove("active")
    btnComentarios.classList.remove("active")
    btnHistorico.classList.remove("active")
    btnAnexos.classList.remove("active")

    btnHistorico.classList.add("active")
  } else if (secao == 'anexos') {
    abaDados.style.display = "none"
    abaHistorico.style.display = "none"
    abaAnexos.style.display = "block"
    abaComentarios.style.display = "none"

    btnDados.classList.remove("active")
    btnComentarios.classList.remove("active")
    btnHistorico.classList.remove("active")
    btnAnexos.classList.remove("active")

    btnAnexos.classList.add("active")
  } else {
    abaDados.style.display = "none"
    abaHistorico.style.display = "none"
    abaAnexos.style.display = "none"
    abaComentarios.style.display = "block"

    btnDados.classList.remove("active")
    btnComentarios.classList.remove("active")
    btnHistorico.classList.remove("active")
    btnAnexos.classList.remove("active")

    btnComentarios.classList.add("active")
  }
}

/**
 * Upload de arquivo via input type="file" e atualização da lista de anexos.
 * Envia o arquivo selecionado para o servidor e atualiza dinamicamente a interface.
*/
document.addEventListener("DOMContentLoaded", () => {
    const input = document.getElementById("file-upload");
    const fileInfo = document.getElementById("file-info");
    const idChamado = document.getElementById("idChamado").value;

    if (!input) return;

    input.addEventListener("change", async () => {
      const file = input.files[0];
      if (!file) return;

      fileInfo.innerText = `Enviando: ${file.name}...`;

      const formData = new FormData();
      formData.append("arquivo", file);
      formData.append("idChamado", idChamado);

      try {
          const response = await fetch("/Chamados/UploadAnexo", {
              method: "POST",
              body: formData
          });

          if (response.ok) {
              fileInfo.innerText = ``;

              // Atualiza lista de anexos via AJAX
              const anexosResponse = await fetch(`/Chamados/ListaAnexos?idChamado=${idChamado}`);
              if (anexosResponse.ok) {
                  const anexosHtml = await anexosResponse.text();
                  document.getElementById("lista-anexos-container").innerHTML = anexosHtml;

                  atualizarHistorico(idChamado)
              }

          } else {
              const erro = await response.text();
              fileInfo.innerText = `❌ Erro: ${erro}`;
          }
      } catch (err) {
          fileInfo.innerText = `❌ Erro inesperado: ${err.message}`;
      }
  });
});

/**
 * Remove um anexo existente após confirmação do usuário.
 * Atualiza a lista de anexos e o histórico após exclusão.
 * @param {number|string} idAnexo - ID do anexo a ser removido
*/
async function removerAnexo(idAnexo) {
  const confirmacao = await Swal.fire({
    title: "Tem certeza?",
    text: "Essa ação vai remover o anexo permanentemente.",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#d33",
    cancelButtonColor: "#3085d6",
    confirmButtonText: "Sim, excluir!",
    cancelButtonText: "Cancelar"
  });

  if (!confirmacao.isConfirmed) return;

  try {
    const response = await fetch("/Chamados/RemoverAnexo", {
      method: "POST",
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      },
      body: `id=${idAnexo}`
    });

    if (response.ok) {
      const idChamado = document.getElementById("idChamado").value;
      const anexosResponse = await fetch(`/Chamados/ListaAnexos?idChamado=${idChamado}`);
      const anexosHtml = await anexosResponse.text();
      document.getElementById("lista-anexos-container").innerHTML = anexosHtml;

      atualizarHistorico(idChamado)

      Swal.fire("Removido!", "O anexo foi excluído com sucesso.", "success");
    } else {
      Swal.fire("Erro", "Não foi possível remover o anexo.", "error");
    }
  } catch (err) {
    console.error("Erro:", err);
    Swal.fire("Erro inesperado", err.message, "error");
  }
}

/**
 * Adiciona um comentário ao chamado.
 * Envia o comentário para o servidor e atualiza a lista de comentários e histórico.
*/
async function adicionarComentario() {
  const texto = document.getElementById('comentario').value;
  const chamadoId = document.getElementById('idChamado').value;

  if (!texto.trim()) return;

  const formData = new URLSearchParams();
  formData.append("chamadoId", chamadoId);
  formData.append("texto", texto);

  // Busca o token antifalsificação (se estiver usando)
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

  try {
    const response = await fetch('/Chamados/SalvarComentario', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        ...(token && { 'RequestVerificationToken': token }) // adiciona o token se existir
      },
      body: formData.toString()
    });

    if (response.ok) {
      // Limpa textarea
      document.getElementById("comentario").value = "";

      // Recarrega comentários
      const comentariosResponse = await fetch(`/Chamados/ListaComentarios?idChamado=${chamadoId}`);
      if (comentariosResponse.ok) {
        const html = await comentariosResponse.text();
        document.getElementById("lista-comentarios-container").innerHTML = html;

        // Atualiza histórico (presumo que essa função esteja definida)
        atualizarHistorico(chamadoId);

      } else {
        alert("Erro ao atualizar os comentários.");
      }
    } else {
      alert("Erro ao salvar o comentário.");
    }
  } catch (error) {
    alert("Erro inesperado: " + error.message);
  }
}

/**
 * Remove um comentário existente após confirmação do usuário.
 * Atualiza a lista de comentários e o histórico após exclusão.
 * @param {number|string} idComentario - ID do comentário a ser removido
*/
async function removerComentario(idComentario) {
  const confirmacao = await Swal.fire({
    title: "Tem certeza?",
    text: "Essa ação vai remover o comentário permanentemente.",
    icon: "warning",
    showCancelButton: true,
    confirmButtonColor: "#d33",
    cancelButtonColor: "#3085d6",
    confirmButtonText: "Sim, excluir!",
    cancelButtonText: "Cancelar"
  });

  if (!confirmacao.isConfirmed) return;

  try {
    const response = await fetch("/Chamados/RemoverComentario", {
      method: "POST",
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      },
      body: `id=${idComentario}`
    });

    if (response.ok) {
      const idChamado = document.getElementById("idChamado").value;
      const comentariosResponse = await fetch(`/Chamados/ListaComentarios?idChamado=${idChamado}`);
      const comentariosHtml = await comentariosResponse.text();
      document.getElementById("lista-comentarios-container").innerHTML = comentariosHtml;

      atualizarHistorico(idChamado)

      Swal.fire("Removido!", "O comentário foi excluído com sucesso.", "success");
    } else {
      Swal.fire("Erro", "Não foi possível remover o comentário.", "error");
    }
  } catch (err) {
    console.error("Erro:", err);
    Swal.fire("Erro inesperado", err.message, "error");
  }
}

/**
 * Atualiza um campo específico do chamado (ex: status, descrição) via AJAX.
 * @param {HTMLElement} elemento - Elemento que contém os atributos data-id e data-campo
*/
async function atualizarCampo(elemento) {
  const id = elemento.dataset.id;
  const campo = elemento.dataset.campo;
  const valor = elemento.value;
  const idChamado = document.getElementById("idChamado").value;

  try {
    const response = await fetch('/Chamados/AtualizarCampo', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      },
      body: JSON.stringify({ id, campo, valor })
    });

    atualizarHistorico(idChamado)

    if (!response.ok) {
      Swal.fire("Erro", "Erro ao atualizar o campo.", "error");
    }
  } catch (error) {
    Swal.fire("Erro", "Erro de rede:", error, "error");
  }
}

/**
 * Atualiza dinamicamente o histórico do chamado.
 * Faz requisição ao servidor para obter HTML atualizado e substitui o conteúdo do container.
 * @param {number|string} idChamado - ID do chamado a ser atualizado
*/
function atualizarHistorico(idChamado) {
  fetch('/Chamados/ListaHistorico?idChamado=' + idChamado)
      .then(response => response.text())
      .then(html => {
          document.getElementById('historico-container').innerHTML = html;
      });
}

/**
 * Configura paginação de cards dentro de um container.
 * Mostra um número definido de cards por página e controla navegação com botões.
 * @param {string} cardsContainerId - ID do container que contém os cards
 * @param {string} prevBtnId - ID do botão "anterior"
 * @param {string} nextBtnId - ID do botão "próximo"
 * @param {string} pageInfoId - ID do elemento que exibe informações de página
 * @param {string} cardClass - Classe CSS que identifica os cards
 * @param {number} [cardsPerPage=5] - Número de cards exibidos por página
*/
function setupPagination(cardsContainerId, prevBtnId, nextBtnId, pageInfoId, cardClass, cardsPerPage = 5) {
  const container = document.getElementById(cardsContainerId);
  const cards = container.querySelectorAll(`.${cardClass}`);
  const totalCards = cards.length;
  const totalPages = Math.ceil(totalCards / cardsPerPage);
  let currentPage = 1;

  const prevBtn = document.getElementById(prevBtnId);
  const nextBtn = document.getElementById(nextBtnId);
  const pageInfo = document.getElementById(pageInfoId);

  function showPage(page) {
    if (page < 1) page = 1;
    if (page > totalPages) page = totalPages;
    currentPage = page;

    cards.forEach((card, i) => {
      card.style.display = (i >= (page - 1) * cardsPerPage && i < page * cardsPerPage) ? 'block' : 'none';
    });

    pageInfo.textContent = `Página ${currentPage} de ${totalPages}`;
    prevBtn.disabled = currentPage === 1;
    nextBtn.disabled = currentPage === totalPages;
  }

  prevBtn.addEventListener('click', () => showPage(currentPage - 1));
  nextBtn.addEventListener('click', () => showPage(currentPage + 1));

  showPage(1);
}
