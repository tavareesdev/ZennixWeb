function validarDataAdmissao() {
  const input = document.getElementById('dataAdmissao');
  const dataAdmissao = new Date(input.value);
  const hoje = new Date();
  hoje.setHours(0,0,0,0); // Ignorar hora

  if (dataAdmissao > hoje) {
    Swal.fire({
      icon: 'error',
      title: 'Data inválida',
      text: 'A data de admissão não pode ser maior que hoje.'
    }).then(() => {
      input.value = '';
      input.focus();
    });
    return false;
  }
  return true;
}

function validarDataNascimento() {
  const input = document.getElementById('dataNascimento');
  const dataNascimento = new Date(input.value);
  const hoje = new Date();
  hoje.setHours(0,0,0,0);

  // Calcula a data mínima (14 anos atrás)
  const dataMinima = new Date(hoje.getFullYear() - 14, hoje.getMonth(), hoje.getDate());

  if (dataNascimento > dataMinima) {
    Swal.fire({
      icon: 'error',
      title: 'Data inválida',
      text: 'A idade mínima deve ser de 14 anos.'
    }).then(() => {
      input.value = '';
      input.focus();
    });
    return false;
  }
  return true;
}

function mascaraTelefone(event) {
  const input = event.target;
  let valor = input.value.replace(/\D/g, ""); // Remove tudo que não for número

  if (valor.length > 11) {
    valor = valor.slice(0, 11);
  }

  // Formata (99) 99999-9999 ou (99) 9999-9999 dependendo do tamanho
  if (valor.length > 10) {
    valor = valor.replace(/^(\d{2})(\d{5})(\d{4}).*/, "($1) $2-$3");
  } else if (valor.length > 5) {
    valor = valor.replace(/^(\d{2})(\d{4})(\d{0,4}).*/, "($1) $2-$3");
  } else if (valor.length > 2) {
    valor = valor.replace(/^(\d{2})(\d{0,5})/, "($1) $2");
  } else if (valor.length > 0) {
    valor = valor.replace(/^(\d{0,2})/, "($1");
  }

  input.value = valor;
}

document.getElementById('btnSalvar').addEventListener('click', async function () {
  // Validações das datas (assumindo que as funções retornam true/false)
  if (!validarDataNascimento() || !validarDataAdmissao()) {
    return;
  }

  // Pega os dados do formulário
  const nome = document.querySelector('input[name="Nome"]').value.trim();
  const email = document.querySelector('input[name="Email"]').value.trim();
  const idCargo = document.querySelector('select[name="ID_Cargo"]').value;
  const dataNascimento = document.querySelector('input[name="dataNascimento"]').value;
  const dataAdmissao = document.querySelector('input[name="dataAdmissao"]').value;
  const telefone = document.querySelector('input[name="telefone"]').value.trim();

  if (!nome || !email || !idCargo || !dataNascimento || !dataAdmissao) {
    Swal.fire({
      icon: 'warning',
      title: 'Atenção',
      text: 'Preencha todos os campos!'
    });
    return;
  }

  // Monta o objeto para enviar (como FormData ou JSON)
  const formData = new FormData();
  formData.append('Nome', nome);
  formData.append('Email', email);
  formData.append('ID_Cargo', idCargo);
  formData.append('dataNascimento', dataNascimento);
  formData.append('dataAdmissao', dataAdmissao);
  formData.append('telefone', telefone);

  try {
    const response = await fetch('@Url.Action("CriarUsuario", "Usuarios")', {
      method: 'POST',
      body: formData
    });

    if (!response.ok) {
      throw new Error('Erro ao salvar usuário');
    }

    const result = await response.json();

    if (result.success) {
      await Swal.fire({
        icon: 'success',
        title: 'Usuário criado com sucesso!',
        timer: 2000,
        showConfirmButton: false
      });
      window.location.href = '@Url.Action("PainelUsuarios", "Usuarios")';
    } else {
      Swal.fire({
        icon: 'error',
        title: 'Erro',
        text: result.message || 'Erro ao criar usuário.'
      });
    }
  } catch (error) {
    Swal.fire({
      icon: 'error',
      title: 'Erro',
      text: error.message
    });
  }
});
