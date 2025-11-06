function toggleMenu() {
    var menu = document.getElementById('menu');
    menu.style.display = (menu.style.display === 'flex') ? 'none' : 'flex';
}

function toggleDropdown(tipo) {
  if(tipo == 'C'){
    const dropdown = document.querySelector('.dropdown-chamado');
    dropdown.classList.toggle('open');
  } else if (tipo == 'U'){
    const dropdown = document.querySelector('.dropdown-user');
    dropdown.classList.toggle('open');
  } else {    
    const dropdown = document.querySelector('.dropdown-exit');
    dropdown.classList.toggle('open');
  }
}
