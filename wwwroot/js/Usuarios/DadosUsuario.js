let bloqueado = false;

document.addEventListener('DOMContentLoaded', function () {
  const input = document.getElementById('fotoInput');
  if (input) {
    input.addEventListener('change', function () {
      const file = this.files[0];
      if (!file || bloqueado) return;

      const validTypes = ['image/jpeg', 'image/jpg'];
      if (!validTypes.includes(file.type)) {
        alert('Apenas arquivos .jpg ou .jpeg são permitidos.');
        return;
      }

      bloqueado = true;
      const formData = new FormData();
      formData.append('foto', file);

      const userId = window.location.pathname.split('/').pop();

      fetch(`/Usuarios/UploadFoto/${userId}`, {
        method: 'POST',
        body: formData
      })
      .then(response => {
        if (!response.ok) throw new Error("Erro ao enviar imagem");

        const img = document.getElementById('fotoPreview');
        const timestamp = new Date().getTime();
        img.src = `/images/usuarios/${userId}.jpg?t=${timestamp}`;
      })
      .catch(error => {
        alert("Erro ao enviar imagem: " + error.message);
      })
      .finally(() => {
        bloqueado = false;
      });
    });
  }

  const statusSelect = document.querySelector('select[name="Status"]');
  const dataDemiInput = document.querySelector('input[name="DataDemi"]');

  function toggleDataDemi() {
    if (statusSelect.value === "1") {
      dataDemiInput.disabled = true;
      dataDemiInput.value = "";
    } else {
      dataDemiInput.disabled = false;
    }
  }

  statusSelect.addEventListener('change', toggleDataDemi);
  toggleDataDemi();
  setupPagination('cards-container', 'prevPage', 'nextPage', 'pageInfo', 'card-historico');
});

// Função de envio dos dados do usuário
document.getElementById('btnSalvar').addEventListener('click', function () {
  if (bloqueado) return;

  bloqueado = true;
  const botao = this;
  botao.disabled = true;

  const campos = {};
  const inputs = document.querySelectorAll('[data-campo]');
  inputs.forEach(input => {
    campos[input.getAttribute('data-campo')] = input.value;
  });

  const idUsuario = window.location.pathname.split('/').pop();

  fetch(`/Usuarios/SalvarAlteracoes?id=${idUsuario}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(campos)
  })
  .then(res => res.ok ? res.json() : Promise.reject(res))
  .then(data => {
    Swal.fire({
      icon: 'success',
      title: 'Sucesso',
      text: data.mensagem || "Alterações salvas com sucesso.",
      timer: 2000,
      showConfirmButton: false
    });
  })
  .catch(err => {
    console.error("Erro ao salvar:", err);
    Swal.fire({
      icon: 'error',
      title: 'Erro',
      text: "Erro ao salvar as alterações.",
      timer: 2500,
      showConfirmButton: false
    });
  })
  .finally(() => {
    bloqueado = false;
    botao.disabled = false;
  });
});

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

function alterarSenha() {
  const nova = document.getElementById('senhaNova').value;
  const confirmacao = document.getElementById('senhaConfirmacao').value;
  const id = document.getElementById('senhaUserId').value;

  if (nova.length < 6) {
    return Swal.fire({ icon: 'warning', text: 'A senha deve ter pelo menos 6 caracteres.' });
  }

  if (nova !== confirmacao) {
    return Swal.fire({ icon: 'warning', text: 'As senhas não coincidem.' });
  }

  fetch(`/Usuarios/AlterarSenha`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ id, novaSenha: nova })
  })
  .then(res => res.ok ? res.json() : Promise.reject(res))
  .then(data => {
    Swal.fire({ icon: 'success', text: data.mensagem || 'Senha alterada com sucesso.' });
    const modal = bootstrap.Modal.getInstance(document.getElementById('modalAlterarSenha'));
    modal.hide();
  })
  .catch(() => {
    Swal.fire({ icon: 'error', text: 'Erro ao alterar senha.' });
  });
}