const { Server } = require('ws');

const server = new Server({ port: 80 }, () => {
    console.log("websocket server started");
});

function init_webrtc(id) {
    const clients = [...server.clients].filter(socket => socket.id === id);
    if (clients.length < 2)
        return;

    const host = clients[0];

    host.offerer = true;
    host.send(JSON.stringify({
        type: "make_offer",
        data: "",
    }));
}

function handler_offer(id, offer) {
    const clients = [...server.clients].filter(socket => socket.id === id && !socket.offerer);
    if (clients.length < 1)
        return;

    const client = clients[0];

    client.answerer = true;
    
    client.send(JSON.stringify({
        type: "make_answer",
        data: offer,
    }));
}

function handler_answer(id, answer) {
    const clients = [...server.clients].filter(socket => socket.id === id && !socket.answerer);
    if (clients.length < 1)
        return;

    const client = clients[0];
    
    client.send(JSON.stringify({
        type: "accept_answer",
        data: answer,
    }));
}

server.on("connection", socket => {
    socket.on("message", data => {
        const message = JSON.parse(data);

        if (message.type === "connect") {
            socket.id = message.data;

            init_webrtc(socket.id);
        }
        if (message.type === "offer") {
            handler_offer(socket.id, message.data);
        }
        if (message.type === "answer") {
            handler_answer(socket.id, message.data);
        }
    });
});