'use strict';

// Éléments du DOM
// Supprimer les éléments liés à la page de connexion manuelle
// const usernamePage = document.querySelector('#username-page');
// const usernameInput = document.querySelector('#username');
// const connectButton = document.querySelector('#connect-button');

const authContainer = document.querySelector('#auth-container');
const loginForm = document.querySelector('#login-form');
const registerForm = document.querySelector('#register-form');
const switchToRegisterLink = document.querySelector('#switch-to-register');
const switchToLoginLink = document.querySelector('#switch-to-login');
const authTitle = document.querySelector('#auth-title');
const authError = document.querySelector('#auth-error');

const mainContainer = document.querySelector('#main-container'); // Conteneur principal du chat
const chatPage = document.querySelector('#chat-page');
const disconnectButton = document.querySelector('#disconnect-button');
const sendButton = document.querySelector('#send-button');
const messageInput = document.querySelector('#message');
// const recipientInput = document.querySelector('#recipient'); // Remplacé par une liste
const messageArea = document.querySelector('#message-area');
const userList = document.querySelector('#user-list'); // Nouvel élément pour la liste des utilisateurs
const channelList = document.querySelector('#channel-list'); // Nouveau
const currentConversationTitle = document.querySelector('#current-conversation-title');

// Éléments pour la création de salon
const createChannelBtn = document.querySelector('#create-channel-btn');
const createChannelModal = document.querySelector('#createChannelModal');
const channelNameInput = document.querySelector('#channel-name');
const channelDescriptionInput = document.querySelector('#channel-description');
const submitChannelBtn = document.querySelector('#submit-channel-btn');
const createChannelError = document.querySelector('#create-channel-error');

// Variables globales
let stompClient = null;
let currentUsername = null; // Sera défini après validation du token
let currentConversation = { type: 'public', id: null, name: 'Chat Public' }; // type: 'public', 'user', 'channel'
let currentSubscriptions = {}; // Pour gérer les abonnements STOMP

// Couleurs pour les avatars (simpliste)
const colors = [
    '#2196F3', '#32c787', '#00BCD4', '#ff5652',
    '#ffc107', '#ff85af', '#FF9800', '#39bbb0'
];

// --- Authentification et Initialisation ---

async function handleLogin(event) {
    event.preventDefault();
    const username = document.querySelector('#login-username').value.trim();
    const password = document.querySelector('#login-password').value.trim();
    hideAuthError();

    if (!username || !password) {
        showAuthError("Veuillez remplir tous les champs.");
        return;
    }

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || "Identifiants invalides");
        }

        console.log("Login réussi:", data);
        // Stocker le token et le nom d'utilisateur
        localStorage.setItem('jwtToken', data.token);
        localStorage.setItem('username', data.username);

        // Démarrer le chat
        initializeChat();

    } catch (error) {
        console.error("Erreur de connexion:", error);
        showAuthError(error.message || "Erreur lors de la connexion.");
    }
}

async function handleRegister(event) {
    event.preventDefault();
    const username = document.querySelector('#register-username').value.trim();
    const email = document.querySelector('#register-email').value.trim();
    const password = document.querySelector('#register-password').value.trim();
    hideAuthError();

    if (!username || !email || !password) {
        showAuthError("Veuillez remplir tous les champs.");
        return;
    }

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || "Erreur lors de l'inscription");
        }

        console.log("Inscription réussie:", data);
        // Stocker le token et le nom d'utilisateur
        localStorage.setItem('jwtToken', data.user.username); // Le nom est dans data.user
        localStorage.setItem('username', data.user.username);

        // Démarrer le chat
        initializeChat();

    } catch (error) {
        console.error("Erreur d'inscription:", error);
        showAuthError(error.message || "Erreur lors de l'inscription.");
    }
}

function showAuthError(message) {
    authError.textContent = message;
    authError.classList.remove('hidden');
}

function hideAuthError() {
    authError.classList.add('hidden');
}

function switchToRegisterView() {
    loginForm.classList.add('hidden');
    registerForm.classList.remove('hidden');
    switchToRegisterLink.classList.add('hidden');
    switchToLoginLink.classList.remove('hidden');
    authTitle.textContent = "Inscription";
    hideAuthError();
}

function switchToLoginView() {
    loginForm.classList.remove('hidden');
    registerForm.classList.add('hidden');
    switchToRegisterLink.classList.remove('hidden');
    switchToLoginLink.classList.add('hidden');
    authTitle.textContent = "Connexion";
    hideAuthError();
}

function initializeChat() {
    const token = localStorage.getItem('jwtToken');
    const storedUsername = localStorage.getItem('username');

    if (token && storedUsername) {
        currentUsername = storedUsername;
        // Cacher l'authentification et afficher le chat
        authContainer.classList.add('hidden');
        mainContainer.classList.remove('hidden');
        connectWebSocket(token);
    } else {
        // Si pas de token, s'assurer que l'écran de login est visible
        authContainer.classList.remove('hidden');
        mainContainer.classList.add('hidden');
        console.log("Pas de session active, affichage de l'écran de connexion.");
    }
}

// --- Connexion WebSocket avec JWT ---
function connectWebSocket(token) {
    console.log("Tentative de connexion WebSocket avec token...");

    const socket = new SockJS('/ws');
    stompClient = Stomp.over(socket);

    // Headers pour la connexion STOMP, incluant le token JWT
    const headers = {
        'Authorization': 'Bearer ' + token
    };

    stompClient.connect(headers, onConnected, onError);
}

function onConnected() {
    console.log('Connecté au serveur WebSocket via STOMP');

    // S'abonner aux messages privés (toujours nécessaire)
    subscribeToTopic('/user/queue/messages');

    // Charger les utilisateurs ET les salons
    fetchUsers();
    fetchChannels();

    // Sélectionner le chat public par défaut
    selectConversation('public', null, 'Chat Public'); 
}

function onError(error) {
    console.error('Erreur de connexion WebSocket:', error);
    alert('Impossible de se connecter au serveur de chat. Vérifiez votre connexion ou réessayez plus tard.');
    // Nettoyer et retourner à l'écran de login
    disconnectCleanup();
}

// --- Gestion des Abonnements STOMP ---
function subscribeToTopic(topic) {
    if (!stompClient) return;
    if (currentSubscriptions[topic]) {
        console.log(`[STOMP] Déjà abonné à ${topic}`);
        return; // Déjà abonné
    }
    console.log(`[STOMP] Tentative d'abonnement à ${topic}`);
    try {
        currentSubscriptions[topic] = stompClient.subscribe(topic, onMessageReceived, { id: topic }); // Utilise le topic comme ID pour simplifier
        console.log(`[STOMP] Abonné avec succès à ${topic}. Abonnements actuels:`, Object.keys(currentSubscriptions));
    } catch (e) {
        console.error(`[STOMP] Erreur lors de l'abonnement à ${topic}:`, e);
    }
}

function unsubscribeFromTopic(topic) {
    if (!stompClient || !currentSubscriptions[topic]) {
         console.log(`[STOMP] Pas d'abonnement trouvé pour ${topic} à supprimer.`);
         return;
    }
    console.log(`[STOMP] Tentative de désabonnement de ${topic}`);
    try {
        currentSubscriptions[topic].unsubscribe();
        delete currentSubscriptions[topic];
        console.log(`[STOMP] Désabonné avec succès de ${topic}. Abonnements restants:`, Object.keys(currentSubscriptions));
    } catch (e) {
        console.error(`[STOMP] Erreur lors du désabonnement de ${topic}:`, e);
        // On supprime quand même la référence locale en cas d'erreur d'unsubscribe
        delete currentSubscriptions[topic]; 
    }
}

// --- Récupération et affichage des utilisateurs (adapté pour utiliser selectConversation) ---
async function fetchUsers() { 
    const token = localStorage.getItem('jwtToken');
    if (!token) return;
    try {
        const response = await fetch('/api/users', { headers: { 'Authorization': 'Bearer ' + token } });
        if (!response.ok) throw new Error(`Erreur HTTP: ${response.status}`);
        const users = await response.json();

        userList.innerHTML = ''; // Vide la liste
        users.forEach(user => {
            const userElement = document.createElement('li');
            userElement.textContent = user.username;
            userElement.classList.add('list-group-item', 'list-group-item-action');
            userElement.dataset.type = 'user';
            userElement.dataset.id = user.username; // Utilise username comme ID pour les users
            userElement.onclick = () => selectConversation('user', user.username, `Conversation avec ${user.username}`);
            userList.appendChild(userElement);
        });
    } catch (error) { console.error("Impossible de récupérer la liste des utilisateurs:", error); }
}

// --- Récupération et affichage des salons --- (Nouveau)
async function fetchChannels() {
    const token = localStorage.getItem('jwtToken');
    if (!token) return;
    try {
        const response = await fetch('/api/channels', { headers: { 'Authorization': 'Bearer ' + token } });
        if (!response.ok) throw new Error(`Erreur HTTP: ${response.status}`);
        const channels = await response.json();

        channelList.innerHTML = ''; // Vide la liste
        // Option Chat Public Global (si on veut la garder séparée des salons)
        const publicOption = document.createElement('li');
        publicOption.textContent = "Chat Public (Général)";
        publicOption.classList.add('list-group-item', 'list-group-item-action', 'active');
        publicOption.dataset.type = 'public';
        publicOption.dataset.id = 'public';
        publicOption.onclick = () => selectConversation('public', null, 'Chat Public (Général)');
        channelList.appendChild(publicOption);

        channels.forEach(channel => {
            const channelElement = document.createElement('li');
            channelElement.textContent = `#${channel.name}`; // Ajoute # pour distinguer
            channelElement.classList.add('list-group-item', 'list-group-item-action');
            channelElement.dataset.type = 'channel';
            channelElement.dataset.id = channel.id;
            channelElement.dataset.name = channel.name;
            channelElement.onclick = () => selectConversation('channel', channel.id, `#${channel.name}`);
            channelList.appendChild(channelElement);
        });
    } catch (error) {
        console.error("Impossible de récupérer la liste des salons:", error);
    }
}

// --- Sélection de conversation et chargement de l'historique (adapté) ---
function selectConversation(type, id, name) {
    console.log(`Sélection: type=${type}, id=${id}, name=${name}`);
    
    // Se désabonner de l'ancien topic de salon si nécessaire
    if (currentConversation.type === 'channel' && currentConversation.id !== null) {
        unsubscribeFromTopic(`/topic/channel.${currentConversation.id}`);
    }

    currentConversation = { type, id, name };

    // S'abonner au nouveau topic de salon si nécessaire
    if (currentConversation.type === 'channel') {
        subscribeToTopic(`/topic/channel.${currentConversation.id}`);
    }
    // L'abonnement à /user/queue/messages est permanent
    // L'abonnement à /topic/public (si on garde un chat public générique) pourrait aussi être permanent

    currentConversationTitle.textContent = name;

    // Met à jour l'UI pour montrer l'élément actif
    document.querySelectorAll('#user-list li, #channel-list li').forEach(li => {
        li.classList.remove('active', 'font-weight-bold', 'text-primary');
        if (li.dataset.type === type && String(li.dataset.id) === String(id)) { // Comparer les ID en string
            li.classList.add('active');
        }
    });

    fetchHistory(); // Appelle fetchHistory qui utilisera currentConversation
}

async function fetchHistory() { 
    messageArea.innerHTML = '';
    const token = localStorage.getItem('jwtToken');
    if (!token) return;

    let url;
    if (currentConversation.type === 'user') {
        url = `/api/messages/user/${currentConversation.id}`; // id est l'username ici
    } else if (currentConversation.type === 'channel') {
        url = `/api/messages/channel/${currentConversation.id}`; // id est le channelId ici
    } else { // public
        url = `/api/messages/public`; // Appeler le nouvel endpoint
    }

    console.log(`Fetching history from: ${url}`); // Log de l'URL appelée

    try {
        const response = await fetch(url, { headers: { 'Authorization': 'Bearer ' + token } });
        if (!response.ok) throw new Error(`Erreur HTTP: ${response.status}`);
        const historyMessages = await response.json();
        console.log("Historique reçu:", historyMessages);
        historyMessages.forEach(displayMessage);
        messageArea.scrollTop = messageArea.scrollHeight;
    } catch (error) {
        console.error(`Impossible de récupérer l'historique pour ${currentConversation.type} ${currentConversation.id}:`, error);
        // Afficher une erreur...
    }
}

// --- Envoi de message (adapté) ---
function sendMessage(event) {
    event.preventDefault();
    const messageContent = messageInput.value.trim();

    if (messageContent && stompClient) {
        const chatMessage = {
            content: messageContent,
            recipientUsername: (currentConversation.type === 'user') ? currentConversation.id : null,
            channelId: (currentConversation.type === 'channel') ? currentConversation.id : null
        };

        stompClient.send("/app/chat.sendMessage", {}, JSON.stringify(chatMessage));
        messageInput.value = '';
    } else if (!stompClient) {
        console.warn("Client STOMP non connecté.");
    }
}

// --- onMessageReceived (adapté) ---
function onMessageReceived(payload) {
    console.log("[onMessageReceived] Payload reçu:", payload);
    let message;
    try { message = JSON.parse(payload.body); console.log("[onMessageReceived] Message parsé:", message); } 
    catch (e) { console.error("[onMessageReceived] Erreur parsing JSON:", e, payload.body); return; }

    // ----- DEBUG: Affichage Forcé ----- 
    console.log("[onMessageReceived] DEBUG: Affichage forcé du message entrant.");
    displayMessage(message);
    // Mettre la ligne suivante en commentaire pour tester la logique normale
    return; 
    // ----- FIN DEBUG -----
    
    /* --- Logique Normale (mise en commentaire pour le debug) ---
    const messageType = message.channelId ? 'channel' : (message.recipientUsername ? 'user' : 'public');
    const messageTargetId = message.channelId ? message.channelId : message.recipientUsername;

    console.log(`[onMessageReceived] Message reçu type=${messageType}, targetId=${messageTargetId}`);
    console.log(`[onMessageReceived] État actuel: currentConversation.type=${currentConversation.type}, currentConversation.id=${currentConversation.id}`);

    // Afficher si le message correspond à la conversation actuelle
    let shouldDisplay = false;
    if (currentConversation.type === messageType) {
        if (messageType === 'channel' && String(currentConversation.id) === String(message.channelId)) { // Comparaison String
            console.log("[onMessageReceived] Condition: Salon actuel match ID du message.");
            shouldDisplay = true;
        } else if (messageType === 'user') {
            console.log(`[onMessageReceived] Condition: Message privé. currentConversation.id=${currentConversation.id}, message.sender=${message.senderUsername}, message.recipient=${message.recipientUsername}`);
            // Est-ce que la conversation affichée concerne le sender OU le recipient de ce message?
            const isCorrectPrivateChat = (currentConversation.id === message.senderUsername || currentConversation.id === message.recipientUsername);
            // Est-ce que JE suis impliqué dans ce message (sender ou recipient)?
            const amIInvolved = (message.senderUsername === currentUsername || message.recipientUsername === currentUsername);
            if(isCorrectPrivateChat && amIInvolved) {
                console.log("[onMessageReceived] Condition: Chat privé actuel match sender/recipient.");
                shouldDisplay = true; 
            }
        } else if (messageType === 'public' && currentConversation.type === 'public') {
            console.log("[onMessageReceived] Condition: Chat public actuel.");
            shouldDisplay = true; 
        }
    }

    if (shouldDisplay) {
        console.log("[onMessageReceived] Affichage du message.");
        displayMessage(message);
    } else {
        console.log("[onMessageReceived] Message ignoré (pas pour la conversation actuelle)");
        // Notification si non affiché
        if (message.senderUsername !== currentUsername) {
            notifyUser(`Nouveau message de ${message.senderUsername}` + (message.channelId ? ` dans #${message.channelName || message.channelId}` : ``));
            // Mettre en évidence l'utilisateur ou le salon
             highlightItemWithNewMessage(messageType, message.channelId || message.senderUsername);
        }
    }
    --- Fin Logique Normale --- */
}

function highlightItemWithNewMessage(type, id) {
    const listId = (type === 'user') ? 'user-list' : 'channel-list';
    document.querySelectorAll(`#${listId} li`).forEach(li => {
        if (li.dataset.type === type && String(li.dataset.id) === String(id) && !li.classList.contains('active')) {
            li.classList.add('font-weight-bold', 'text-primary');
        }
    });
}

// --- displayMessage (adapté pour afficher le nom du salon si pertinent) ---
function displayMessage(message) { 
    const messageElement = document.createElement('div');
    messageElement.classList.add('message');
    if (message.senderUsername === currentUsername) {
        messageElement.classList.add('sent');
    } else {
        messageElement.classList.add('received');
    }
    const avatarElement = document.createElement('span');
    const avatarText = document.createTextNode(message.senderUsername[0]);
    avatarElement.appendChild(avatarText);
    avatarElement.style['background-color'] = getAvatarColor(message.senderUsername);
    avatarElement.style['color'] = '#fff';
    avatarElement.style['padding'] = '5px 8px';
    avatarElement.style['border-radius'] = '50%';
    avatarElement.style['margin-right'] = '8px';
    messageElement.appendChild(avatarElement);

    const infoElement = document.createElement('div');
    infoElement.classList.add('sender');
    // Affiche le nom du salon si c'est un message de salon et qu'on n'est PAS dans ce salon
    let prefix = "";
    if (message.channelId && currentConversation.type !== 'channel' || currentConversation.id !== message.channelId) {
       // Pourrait récupérer le nom du channel ici si besoin (pas dans le DTO actuel)
       prefix = `(#${message.channelId}) `; 
    }
    infoElement.textContent = prefix + message.senderUsername;
    messageElement.appendChild(infoElement);

    const textElement = document.createElement('p');
    textElement.textContent = message.content;
    messageElement.appendChild(textElement);

    const timeElement = document.createElement('span');
    timeElement.classList.add('timestamp');
    const messageTime = new Date(message.timestamp);
    timeElement.textContent = messageTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    messageElement.appendChild(timeElement);

    messageArea.appendChild(messageElement);
    messageArea.scrollTop = messageArea.scrollHeight;
}

// --- Déconnexion (adapté pour nettoyer les abonnements) ---
function disconnect(event) {
    if(event) event.preventDefault(); 
    if (stompClient !== null) {
        console.log("Déconnexion STOMP et nettoyage des abonnements...");
        // Se désabonner de tous les topics avant de déconnecter
        Object.keys(currentSubscriptions).forEach(topic => {
            try { currentSubscriptions[topic].unsubscribe(); } catch(e){}
        });
        currentSubscriptions = {};
        try {
             stompClient.disconnect(() => { console.log("STOMP Déconnecté"); });
        } catch (e) { console.warn("Erreur lors de la déconnexion STOMP:", e); }
        stompClient = null;
    }
    disconnectCleanup();
}

// Fonction séparée pour le nettoyage UI/localStorage
function disconnectCleanup() {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('username');
    currentUsername = null;
    currentConversation = { type: 'public', id: null, name: 'Chat Public' };
    currentSubscriptions = {};
    mainContainer.classList.add('hidden');
    authContainer.classList.remove('hidden');
    switchToLoginView(); // S'assure que le formulaire de login est visible
    userList.innerHTML = '<li class="list-group-item">Déconnecté</li>'; // Vide la liste des users
    messageArea.innerHTML = ''; // Vide les messages
    console.log("Interface et session nettoyées.");
}

// --- Fonctions utilitaires (getAvatarColor, notifyUser) ---
function getAvatarColor(messageSender) {
    let hash = 0;
    for (let i = 0; i < messageSender.length; i++) {
        hash = 31 * hash + messageSender.charCodeAt(i);
    }
    const index = Math.abs(hash % colors.length);
    return colors[index];
}
function notifyUser(message) {
    if (!("Notification" in window)) { return; }
    if (Notification.permission === "granted") {
        new Notification(message);
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(permission => {
            if (permission === "granted") { new Notification(message); }
        });
    }
}

// --- Initialisation et Écouteurs d'événements ---
document.addEventListener('DOMContentLoaded', () => {
    // Essayer d'initialiser le chat si un token existe
    initializeChat(); 

    // Écouteurs pour les formulaires d'authentification
    if(loginForm) loginForm.addEventListener('submit', handleLogin);
    if(registerForm) registerForm.addEventListener('submit', handleRegister);
    if(switchToRegisterLink) switchToRegisterLink.addEventListener('click', switchToRegisterView);
    if(switchToLoginLink) switchToLoginLink.addEventListener('click', switchToLoginView);

    // Écouteurs pour le chat (seront actifs seulement si le chat est visible)
    if(disconnectButton) disconnectButton.addEventListener('click', disconnect);
    if(sendButton) sendButton.addEventListener('click', sendMessage);
    // Gérer l'envoi avec la touche Entrée
    if(messageInput) {
        messageInput.addEventListener('keypress', function(event) {
            if (event.key === 'Enter') {
                sendMessage(event);
            }
        });
    }

    // Gestion des événements pour la création de salon
    createChannelBtn.addEventListener('click', showCreateChannelModal, true);
    submitChannelBtn.addEventListener('click', createChannel, true);

    // Vérifier l'authentification au chargement
    checkAuth();
});

// Cache la page de chat initialement (si non géré par la logique d'authentification)
// chatPage.classList.add('hidden');

// --- Fonction pour afficher un message dans le DOM ---
function displayMessage(message) {
    const messageElement = document.createElement('div');
    messageElement.classList.add('message');

    // Détermine si le message est envoyé ou reçu par l'utilisateur actuel
    if (message.senderUsername === currentUsername) {
        messageElement.classList.add('sent');
    } else {
        messageElement.classList.add('received');
    }

    // Avatar simplifié (initiale avec couleur)
    const avatarElement = document.createElement('span');
    const avatarText = document.createTextNode(message.senderUsername[0]);
    avatarElement.appendChild(avatarText);
    avatarElement.style['background-color'] = getAvatarColor(message.senderUsername);
    avatarElement.style['color'] = '#fff';
    avatarElement.style['padding'] = '5px 8px';
    avatarElement.style['border-radius'] = '50%';
    avatarElement.style['margin-right'] = '8px';
    // messageElement.appendChild(avatarElement); // Décommentez pour afficher l'avatar

    // Informations du message
    const infoElement = document.createElement('div');
    infoElement.classList.add('sender');
    infoElement.textContent = message.senderUsername + (message.recipientUsername ? ' à ' + message.recipientUsername : '');
    messageElement.appendChild(infoElement);

    // Contenu du message
    const textElement = document.createElement('p');
    textElement.textContent = message.content;
    messageElement.appendChild(textElement);

    // Timestamp
    const timeElement = document.createElement('span');
    timeElement.classList.add('timestamp');
    // Formater le timestamp (simpliste)
    const messageTime = new Date(message.timestamp);
    timeElement.textContent = messageTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    messageElement.appendChild(timeElement);

    // Ajoute le message à la zone de chat
    messageArea.appendChild(messageElement);
    // Fait défiler vers le bas pour voir le dernier message
    messageArea.scrollTop = messageArea.scrollHeight;

    // Notification (très basique)
    if (document.hidden && message.senderUsername !== currentUsername) { // Si l'onglet n'est pas actif
        notifyUser("Nouveau message de " + message.senderUsername);
    }
}

// --- Fonctions pour la création de salon ---
function showCreateChannelModal() {
    // Réinitialiser le formulaire
    document.getElementById('create-channel-form').reset();
    createChannelError.textContent = '';
    createChannelError.style.display = 'none';
    
    // Afficher le modal
    const modal = new bootstrap.Modal(createChannelModal);
    modal.show();
}

async function createChannel() {
    // Récupérer les valeurs du formulaire
    const name = channelNameInput.value.trim();
    const description = channelDescriptionInput.value.trim();
    
    // Validation basique
    if (!name) {
        createChannelError.textContent = 'Le nom du salon est requis';
        createChannelError.style.display = 'block';
        return;
    }
    
    // Préparer les données
    const channelData = {
        name: name,
        description: description
    };
    
    // Envoyer la requête au serveur
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        createChannelError.textContent = 'Vous devez être connecté pour créer un salon';
        createChannelError.style.display = 'block';
        return;
    }
    
    try {
        const response = await fetch('/api/channels', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + token
            },
            body: JSON.stringify(channelData)
        });
        
        // Gérer la réponse
        if (response.ok) {
            // Fermer le modal
            const modal = bootstrap.Modal.getInstance(createChannelModal);
            modal.hide();
            
            // Rafraîchir la liste des salons
            fetchChannels();
            
            // Optionnel: sélectionner le nouveau salon automatiquement
            const newChannel = await response.json();
            selectConversation('channel', newChannel.id, `#${newChannel.name}`);
        } else {
            // Afficher l'erreur
            const errorData = await response.json();
            createChannelError.textContent = errorData || 'Erreur lors de la création du salon';
            createChannelError.style.display = 'block';
        }
    } catch (error) {
        console.error('Erreur lors de la création du salon:', error);
        createChannelError.textContent = 'Erreur lors de la création du salon';
        createChannelError.style.display = 'block';
    }
} 