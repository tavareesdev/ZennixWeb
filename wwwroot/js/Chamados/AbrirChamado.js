/**
    * Script de gerenciamento de anexos e validação de formulário de abertura de chamado
    *
    * Funcionalidades:
    * - Permite adicionar múltiplos arquivos dinamicamente
    * - Exibe lista de arquivos adicionados com botão para remover cada um
    * - Evita duplicação de arquivos
    * - Atualiza o input de arquivos para refletir a lista atual
    * - Valida campos obrigatórios do formulário antes de enviar
*/

document.addEventListener("DOMContentLoaded", () => {
    const btnAdicionarAnexo = document.getElementById("btnAdicionarAnexo");
    let inputAnexo = document.getElementById("inputAnexo");
    const anexosBox = document.getElementById("anexosBox");

    let arquivosSelecionados = [];

    function atualizarExibicaoAnexos() {
        anexosBox.innerHTML = "";

        arquivosSelecionados.forEach((file, index) => {
            const item = document.createElement("div");
            item.classList.add("anexo-item");

            const span = document.createElement("span");
            span.textContent = file.name;

            const btnRemover = document.createElement("button");
            btnRemover.type = "button";
            btnRemover.textContent = "✖";
            btnRemover.classList.add("btn-remover-anexo");

            btnRemover.addEventListener("click", () => {
                arquivosSelecionados.splice(index, 1);
                atualizarExibicaoAnexos();
                atualizarInputFiles();
            });

            item.appendChild(span);
            item.appendChild(btnRemover);
            anexosBox.appendChild(item);
        });
    }

    function atualizarInputFiles() {
        // Criar novo input
        const novoInput = document.createElement("input");
        novoInput.type = "file";
        novoInput.name = "inputAnexo";
        novoInput.id = "inputAnexo";
        novoInput.multiple = true;
        novoInput.hidden = true;

        const dataTransfer = new DataTransfer();
        arquivosSelecionados.forEach(file => dataTransfer.items.add(file));
        novoInput.files = dataTransfer.files;

        // Substitui o input antigo
        inputAnexo.replaceWith(novoInput);
        inputAnexo = novoInput;

        // Reanexa o listener
        inputAnexo.addEventListener("change", onFileChange);
    }

    function onFileChange() {
        Array.from(inputAnexo.files).forEach(file => {
            const duplicado = arquivosSelecionados.some(f => f.name === file.name && f.size === file.size);
            if (!duplicado) {
                arquivosSelecionados.push(file);
            }
        });

        atualizarExibicaoAnexos();
        atualizarInputFiles();
    }

    // Inicializa
    inputAnexo.addEventListener("change", onFileChange);

    btnAdicionarAnexo.addEventListener("click", () => {
        inputAnexo.click();
    });
});

/**
    * Validação e submissão do formulário de abertura de chamado
    * - Verifica campos obrigatórios
    * - Exibe alert estilizado com SweetAlert se algum campo estiver vazio
    * - Submete formulário se todos os campos estiverem preenchidos
*/

function abrirChamado() {
    let msg = "";

    if (document.getElementById("tituloChamado").value.trim() === "") {
        msg += "- Título<br>";
    }
    if (document.getElementById("solicitante").value.trim() === "") {
        msg += "- Solicitante<br>";
    }
    if (document.getElementById("setorSolicitante").value.trim() === "") {
        msg += "- Setor Solicitante<br>";
    }
    if (document.getElementById("setorSolicitado").value.trim() === "") {
        msg += "- Setor Solicitado<br>";
    }
    if (document.getElementById("descricao").value.trim() === "") {
        msg += "- Descrição<br>";
    }

    if (msg !== "") {
        Swal.fire({
            icon: 'error',
            title: 'Preencha os campos abaixo!',
            html: msg // <-- usando html ao invés de text
        });
    } else {
        document.getElementById("formChamado").submit();
    }
}