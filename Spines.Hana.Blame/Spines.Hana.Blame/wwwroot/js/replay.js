﻿var replayContext;
var _observedPlayerId;
var _replay;

// if an input goes above or below the max/min value, adjusts that.
function wrapFrameInputValues(d) {
    // wrap seats around
    if (!d.p) {
        d.p = 0;
    }
    if (d.p < 0) {
        const x = d.p % 4;
        d.p = 4 + x;
    }
    if (d.p > 3) {
        d.p = d.p % 4;
    }

    if (!_replay) {
        return d;
    }

    // limit games to (0, max)
    if (!d.g) {
        d.g = 0;
    }
    if (d.g < 0) {
        d.g = 0;
    }
    if (d.g >= _replay.length) {
        d.g = _replay.length - 1;
    }

    // wrap into next/previous game if possible
    if (!d.f) {
        d.f = 0;
    }
    if (d.f < 0) {
        if (d.g > 0) {
            d.g -= 1;
            d.f = _replay[d.g].frames.length - 1;
        } else {
            d.f = 0;
        }
    }
    if (d.f >= _replay[d.g].frames.length) {
        if (d.g < _replay.length - 1) {
            d.g += 1;
            d.f = 0;
        } else {
            d.f = _replay[d.g].frames.length - 1;
        }
    }

    return d;
}

function loadReplay(d) {
    if (!d.r || !_replayIdRegex.test(d.r)) {
        _replay = undefined;
        setFrameInputData(d);
        replayContext.createTiles(() => arrange());
        return;
    }

    const replayId = d.r;

    const xhr = $.ajax({
        type: "GET",
        url: "/Api/Replay",
        data: `replayId=${replayId}`,
        success: function (data, textStatus, xhr2) {
            if (xhr2.replayId === _replayId) {
                _replay = parseReplay(JSON.parse(data));
                setFrameInputData(d);
                replayContext.createTiles(() => arrange());
            }
        }
    });
    xhr.replayId = d.r;
}

function initReplay() {
    replayContext = new RenderContext("replayCanvas");
    replayContext.setCameraPosition(_cameraPosition, _lookAt);
    replayContext.createAmbientLight(_ambientLightColor);
    replayContext.createPointLight(_pointLightColor, _pointLightPosition);
}

function arrange() {
    if (!_replay) {
        return;
    }

    const urlParams = wrapFrameInputValues(getUrlParams());
    const playerId = urlParams.p;
    const game = urlParams.g;
    const frame = urlParams.f;

    if (playerId < 0 || playerId > 3) {
        return;
    }
    _observedPlayerId = playerId;

    if (game < _replay.length && game >= 0) {
        if (frame < _replay[game].frames.length && frame >= 0) {
            arrangeFrame(_replay[game].frames[frame]);
        }
    }
}

function arrangeFrame(frame) {
    createWall(frame);
    createHands(frame);
    createPonds(frame);
    createAnnouncements(frame);
    createBa(frame);
    createPlayerInfos(frame);
}

function createPlayerInfos(frame) {
    const staticPlayers = frame.static.players;
    const players = frame.players;
    const playerCount = frame.static.playerCount;
    for (let playerId = 0; playerId < playerCount; playerId++) {
        createPlayerInfo(staticPlayers[playerId], players[playerId], playerId);
    }
}

function createPlayerInfo(staticPlayer, player, playerId) {
    const p = getRotatedPlayerId(playerId);
    const div = document.querySelector(`#playerInfo${p}`);
    div.textContent =
        staticPlayer.name + "\r\n" +
        staticPlayer.gender + " " + _ranks[staticPlayer.rank] + " R" + staticPlayer.rate + "\r\n" +
        player.score;
}

function createInitialPlayer(s) {
    return {
        riichi: false,
        payment: undefined,
        score: s,
        disconnected: false
    };
}

function createInitialPlayers(gameData) {
    return [
        createInitialPlayer(gameData.scores[0] * 100),
        createInitialPlayer(gameData.scores[1] * 100),
        createInitialPlayer(gameData.scores[2] * 100),
        createInitialPlayer(gameData.scores[3] * 100)
    ];
}

function parseReplay(data) {
    const defaultDoraIndicatorCount = 1;
    const akaDora = true;

    const playerCount = data.players.length;
    const players = data.players;
   
    const games = [];
    let gameCount = data.games.length;
    for (let gameId = 0; gameId < gameCount; gameId++) {
        var gameData = data.games[gameId];
        const game = {};
        game.frames = [];
        const setupFrame = {};
        setupFrame.id = 0;
        setupFrame.tilesDrawn = 13 * 4;
        setupFrame.rinshanTilesDrawn = 0;
        setupFrame.doraIndicators = defaultDoraIndicatorCount;
        setupFrame.static = { wall: gameData.wall, dice: gameData.dice, akaDora: akaDora, playerCount: playerCount, oya: gameData.oya, players: players };
        setStartingHands(setupFrame);
        setupFrame.ponds = [[], [], [], []];
        setupFrame.players = createInitialPlayers(gameData);
        game.frames.push(setupFrame);

        let previousFrame = setupFrame;
        const decisions = gameData.actions;
        const decisionCount = decisions.length;
        let callCount = 0;
        let agariCount = 0;
        let discardCount = 0;
        for (let decisionId = 0; decisionId < decisionCount; decisionId++) {
            const decision = decisions[decisionId];

            if (decision === _ids.draw) {
                const frame = createDrawFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.discard) {
                const discard = gameData.discards[discardCount];
                const frame = createDiscardFrame(previousFrame, discard);
                discardCount += 1;
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.pon) {
                const tiles = gameData.calls[callCount].tiles;
                callCount += 1;
                const frames = createCallFrames(previousFrame, tiles, announcements.pon);
                game.frames.push(frames[0], frames[1]);
                previousFrame = frames[1];
            } else if (decision === _ids.chii) {
                const tiles = gameData.calls[callCount].tiles;
                callCount += 1;
                const frames = createCallFrames(previousFrame, tiles, announcements.chii);
                game.frames.push(frames[0], frames[1]);
                previousFrame = frames[1];
            } else if (decision === _ids.calledKan) {
                const tiles = gameData.calls[callCount].tiles;
                callCount += 1;
                const frames = createCallFrames(previousFrame, tiles, announcements.kan);
                game.frames.push(frames[0], frames[1]);
                previousFrame = frames[1];
            } else if (decision === _ids.closedKan) {
                const tiles = gameData.calls[callCount].tiles;
                callCount += 1;
                const frames = createClosedKanFrames(previousFrame, tiles, announcements.kan);
                game.frames.push(frames[0], frames[1]);
                previousFrame = frames[1];
            } else if (decision === _ids.addedKan) {
                const tiles = gameData.calls[callCount].tiles;
                callCount += 1;
                const frames = createAddedKanFrames(previousFrame, tiles, announcements.kan);
                game.frames.push(frames[0], frames[1]);
                previousFrame = frames[1];
            } else if (decision === _ids.ron || decision === _ids.tsumo) {
                const agari = gameData.agaris[agariCount];
                agariCount += 1;
                const agariFrame = createAgariFrame(previousFrame, agari);
                game.frames.push(agariFrame);
                previousFrame = agariFrame;
            } else if (isRyuukyokuDecision(decision)) {
                const frame = createRyuukyokuFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.riichi) {
                const frame = createRiichiFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.riichiPayment) {
                const frame = createRiichiPaymentFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.dora) {
                const frame = createDoraFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision === _ids.rinshan) { 
                const frame = createRinshanFrame(previousFrame);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision >= _ids.disconnectBase && decision < _ids.disconnectBase + 4) {
                const frame = createDisconnectFrame(previousFrame, decision - _ids.disconnectBase);
                game.frames.push(frame);
                previousFrame = frame;
            } else if (decision >= _ids.reconnectBase && decision < _ids.reconnectBase + 4) {
                const frame = createReconnectFrame(previousFrame, decision - _ids.reconnectBase);
                game.frames.push(frame);
                previousFrame = frame;
            }
        }

        games.push(game);
    }
    return games;
}

function isRyuukyokuDecision(decision) {
    return decision === _ids.exhaustiveDraw ||
        decision === _ids.fourKan ||
        decision === _ids.fourRiichi ||
        decision === _ids.fourWind ||
        decision === _ids.nagashiMangan ||
        decision === _ids.nineYaochuuHai ||
        decision === _ids.threeRon;
}

function createDisconnectFrame(previousFrame, who) {
    const frame = cloneFrame(previousFrame);
    frame.players = frame.players.slice(0);
    frame.players[who] = Object.assign({}, frame.players[who]);
    frame.players[who].disconnected = true;
    return frame;
}

function createReconnectFrame(previousFrame, who) {
    const frame = cloneFrame(previousFrame);
    frame.players = frame.players.slice(0);
    frame.players[who] = Object.assign({}, frame.players[who]);
    frame.players[who].disconnected = false;
    return frame;
}

function createRyuukyokuFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.announcement = announcements.ryuukyoku;
    return frame;
}

function createAgariFrame(previousFrame, agari) {
    const frame = cloneFrame(previousFrame);
    const who = agari.winner;
    const fromWho = agari.from;
    frame.players = frame.players.slice(0);
    frame.players[who] = Object.assign({}, frame.players[who]);
    if (who === fromWho) {
        frame.players[who].announcement = announcements.tsumo;
    } else {
        frame.players[who].announcement = announcements.ron;
    }
    return frame;
}

function createRinshanFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.rinshanTilesDrawn += 1;
    const count = frame.rinshanTilesDrawn;
    const index = count % 2 ? count : count - 2;
    const drawnTileId = frame.static.wall[index];
    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[frame.activePlayer]);
    hand.tiles = hand.tiles.slice(0);
    hand.tiles.push(drawnTileId);
    hand.drewRinshan = false;
    frame.hands[frame.activePlayer] = hand;
    return frame;
}

function createDoraFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.doraIndicators += 1;
    return frame;
}

function createAddedKanFrames(previousFrame, meldedTiles, announcement) {
    const activePlayer = previousFrame.activePlayer;

    const announcementFrame = cloneFrame(previousFrame);
    announcementFrame.players = announcementFrame.players.slice(0);
    announcementFrame.players[activePlayer] = Object.assign({}, announcementFrame.players[announcementFrame.activePlayer]);
    announcementFrame.players[activePlayer].announcement = announcement;

    const frame = cloneFrame(announcementFrame);

    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[frame.activePlayer]);
    hand.tiles = hand.tiles.slice(0);

    removeMany(hand.tiles, meldedTiles);
    hand.justCalled = true;
    frame.hands[frame.activePlayer] = hand;

    hand.melds = hand.melds.slice(0);

    const meldCount = hand.melds.length;
    for (let i = 0; i < meldCount; i++) {
        const pon = hand.melds[i];
        if (pon.tiles.some(t => meldedTiles.indexOf(t) !== -1)) {
            const added = meldedTiles.find(t => pon.tiles.indexOf(t) === -1);
            const shape = 7 + 9 + getTileNumberIndex(added);
            const suit = suitFromTileId(meldedTiles[0]);
            hand.melds[i] = { tiles: meldedTiles, flipped: pon.flipped, added: added, relativeFrom: pon.relativeFrom, shape: shape, suit: suit };
            break;
        }
    }

    return [announcementFrame, frame];
}

function createClosedKanFrames(previousFrame, meldedTiles, announcement) {
    const activePlayer = previousFrame.activePlayer;

    const announcementFrame = cloneFrame(previousFrame);
    announcementFrame.players = announcementFrame.players.slice(0);
    announcementFrame.players[activePlayer] = Object.assign({}, announcementFrame.players[announcementFrame.activePlayer]);
    announcementFrame.players[activePlayer].announcement = announcement;

    const frame = cloneFrame(announcementFrame);

    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[frame.activePlayer]);
    hand.tiles = hand.tiles.slice(0);

    removeMany(hand.tiles, meldedTiles);
    hand.justCalled = true;
    frame.hands[frame.activePlayer] = hand;

    const shape = 7 + 9 + getTileNumberIndex(meldedTiles[0]);
    const suit = suitFromTileId(meldedTiles[0]);
    hand.melds = hand.melds.slice(0);
    hand.melds.push({ tiles: meldedTiles, relativeFrom: 0, shape: shape, suit: suit });

    return [announcementFrame, frame];
}

function createCallFrames(previousFrame, meldedTiles, announcement) {
    const playerCalledFrom = previousFrame.activePlayer;
    const activePlayer = getCallingPlayerId(previousFrame, meldedTiles);

    const announcementFrame = cloneFrame(previousFrame);
    announcementFrame.activePlayer = activePlayer;
    announcementFrame.players = announcementFrame.players.slice(0);
    announcementFrame.players[activePlayer] = Object.assign({}, announcementFrame.players[activePlayer]);
    announcementFrame.players[activePlayer].announcement = announcement;

    const frame = cloneFrame(announcementFrame);
    frame.activeDiscardPlayerId = undefined;

    frame.ponds = frame.ponds.slice(0);
    const pond = frame.ponds[playerCalledFrom].slice(0);
    const called = pond.pop();
    const ghostTile = Object.assign({}, called);
    pond.push(ghostTile);
    frame.ponds[playerCalledFrom] = pond;

    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[activePlayer]);
    hand.tiles = hand.tiles.slice(0);

    removeMany(hand.tiles, meldedTiles);
    hand.justCalled = true;
    frame.hands[activePlayer] = hand;

    const min = Math.min(...meldedTiles.map(getTileNumberIndex));
    const ponOffset = announcement === announcements.pon ? 7 : 0;
    const kanOffset = meldedTiles.length === 4 ? 7 + 9 : 0;
    const shape = ponOffset + kanOffset + min;
    const suit = suitFromTileId(meldedTiles[0]);

    hand.melds = hand.melds.slice(0);
    const relativeFrom = (playerCalledFrom - activePlayer + 4) % 4;
    const meld = { tiles: meldedTiles, flipped: called.tileId, relativeFrom: relativeFrom, shape: shape, suit: suit };
    hand.melds.push(meld);

    ghostTile.meld = meld;

    return [announcementFrame, frame];
}

function getTileNumberIndex(tileId) {
    return (tileId >> 2) % 9;
}

function createRiichiFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.players = frame.players.slice(0);
    frame.players[frame.activePlayer] = Object.assign({}, frame.players[frame.activePlayer]);
    frame.players[frame.activePlayer].announcement = announcements.riichi;
    frame.players[frame.activePlayer].riichi = true;
    return frame;
}

function createRiichiPaymentFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.players = frame.players.slice(0);
    const activePlayer = frame.activePlayer;
    frame.players[activePlayer] = Object.assign({}, frame.players[activePlayer]);
    frame.players[activePlayer].payment = 1000;
    frame.players[activePlayer].score -= 1000;
    return frame;
}

function createDiscardFrame(previousFrame, discard) {
    const frame = cloneFrame(previousFrame);
    frame.activeDiscardPlayerId = frame.activePlayer;
    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[frame.activePlayer]);
    hand.tiles = hand.tiles.slice(0);
    remove(hand.tiles, discard);
    intSort(hand.tiles);
    hand.justCalled = false;
    hand.drewRinshan = false;
    frame.hands[frame.activePlayer] = hand;
    frame.ponds = frame.ponds.slice(0);
    const pond = frame.ponds[frame.activePlayer].slice(0);
    const flipped = frame.players[frame.activePlayer].riichi && !pond.some(p => p.flipped && !p.meld);
    pond.push({ tileId: discard, flipped: flipped });
    frame.ponds[frame.activePlayer] = pond;
    return frame;
}

function createDrawFrame(previousFrame) {
    const frame = cloneFrame(previousFrame);
    frame.activeDiscardPlayerId = undefined;
    frame.tilesDrawn += 1;
    if (frame.activePlayer === undefined) {
        frame.activePlayer = frame.static.oya;
    } else {
        frame.activePlayer = (frame.activePlayer + 1) % 4;
    }
    const drawnTileId = frame.static.wall[136 - frame.tilesDrawn];
    frame.hands = frame.hands.slice(0);
    const hand = Object.assign({}, frame.hands[frame.activePlayer]);
    hand.tiles = hand.tiles.slice(0);
    hand.tiles.push(drawnTileId);
    frame.hands[frame.activePlayer] = hand;
    return frame;
}

function cloneFrame(frame) {
    const clone = Object.assign({}, frame);
    clone.id += 1;
    if (clone.players.some(p => p.announcement)) {
        clone.players = frame.players.slice(0);
        const playerCount = frame.static.playerCount;
        for (let i = 0; i < playerCount; i++) {
            if (clone.players[i].announcement) {
                clone.players[i] = Object.assign({}, clone.players[i]);
                clone.players[i].announcement = undefined;
            }
        }
    }
    frame.announcement = undefined;
    return clone;
}

function getCallingPlayerId(frame, meldedTiles) {
    const playerCount = frame.static.playerCount;
    for (let i = 0; i < playerCount; i++) {
        if (meldedTiles.some(x => frame.hands[i].tiles.indexOf(x) !== -1)) {
            return i;
        }
    }
    throw "no player found for call";
}

function createHands(frame) {
    const a = -(14 * tileWidth + gap) / 2;
    const b = -(11 * tileWidth);
    const y = b - 0.5 * tileHeight;
    const handStartX = a + 0.5 * tileWidth - tileWidth;
    const meldStartX = -a + 4 * tileWidth;

    const tileCount = frame.hands.length;
    for (let i = 0; i < tileCount; i++) {
        let x = handStartX;
        const hand = frame.hands[i];
        const tilesInHand = hand.tiles.length;
        for (let k = 0; k < tilesInHand; k++) {
            const tileId = hand.tiles[k];
            addTile(i, tileId, x, y, 0, _tilePlacement.hand);
            if (!frame.hands[i].justCalled && k === tilesInHand - 2 && tilesInHand % 3 === 2) {
                x += gap;
            }
            x += tileWidth;
        }

        let meldX = meldStartX;
        const meldCount = hand.melds.length;
        for (let meldId = 0; meldId < meldCount; meldId++) {
            const meld = hand.melds[meldId];
            meldX = createMeld(i, meldX, meld);
        }
    }
}

function createPonds(frame) {
    const playerCount = frame.static.playerCount;
    for (let playerId = 0; playerId < playerCount; playerId++) {
        const pond = frame.ponds[playerId].filter(p => showGhostTiles || p.meld === undefined);
        const gapOnLastTile = frame.activeDiscardPlayerId === playerId;
        createPondRow(pond.slice(0, 6), 0, playerId, pond.length <= 6 && gapOnLastTile);
        createPondRow(pond.slice(6, 12), 1, playerId, pond.length <= 12 && gapOnLastTile);
        createPondRow(pond.slice(12), 2, playerId, gapOnLastTile);
    }
}

function createPondRow(pondRow, row, playerId, gapOnLastTile) {
    const a = -(3 * tileWidth);
    var x = a + 0.5 * tileWidth;
    var y = a - 0.5 * tileHeight - row * tileHeight;
    const tileCount = pondRow.length;
    for (let column = 0; column < tileCount; column++) {
        const pondTile = pondRow[column];
        const tileId = pondTile.tileId;
        x += pondTile.flipped ? tileHeight - tileWidth : 0;
        if (gapOnLastTile && column === tileCount - 1) {
            x += 0.1;
            y -= 0.1;
        }
        if (pondTile.meld) {
            const placement = pondTile.flipped ? _tilePlacement.pondGhostFlipped : _tilePlacement.pondGhost;
            addGhostTile(playerId, tileId, x, y, 0, placement);
        } else {
            const placement = pondTile.flipped ? _tilePlacement.pondFlipped : _tilePlacement.pond;
            addTile(playerId, tileId, x, y, 0, placement);
        }
        x += tileWidth;
    }
}

function createBa(frame) {

    const playerCount = frame.static.playerCount;
    for (let i = 0; i < playerCount; i++) {
        if (frame.players[i].payment === undefined) {
            continue;
        }
        const mesh = replayContext.createBaMesh(frame.players[i].payment);
        const rotatedSeat = getRotatedPlayerId(i);
        mesh.rotateZ(Math.PI * 0.5 * rotatedSeat);
        mesh.translateY(-1.5);
        mesh.rotateY(Math.PI * 0.5);
        mesh.rotateZ(Math.PI * 0.5);
        replayContext.addBa(mesh);
    }
}

function createAnnouncements(frame) {
    const playerCount = frame.static.playerCount;
    for (let playerId = 0; playerId < playerCount; playerId++) {
        createAnnouncement(frame.players[playerId].announcement, playerId);
    }
    createAnnouncement(frame.announcement);
}

function createAnnouncement(announcement, playerId) {
    if (!announcement) {
        return;
    }
    if (!announcement.geometry) {
        const text = announcement.text;
        const geometry = new THREE.TextBufferGeometry(text, { font: font, size: 2, height: 0, curveSegments: 2 });
        geometry.computeBoundingBox();
        announcement.geometry = geometry;
    }
    const g = announcement.geometry;
    const boundingBox = g.boundingBox;
    const x = -0.5 * (boundingBox.max.x - boundingBox.min.x);
    const y = -0.5 * (boundingBox.max.y - boundingBox.min.y);
    const z = 2;
    const mesh = new THREE.Mesh(g, announcements.material);
    mesh.position.x = x;
    mesh.position.y = y;
    mesh.position.z = z;

    if (playerId !== undefined) {
        const p = getRotatedPlayerId(playerId);
        mesh.rotateZ(Math.PI * 0.5 * p);
        mesh.translateY(-7);
        mesh.rotateZ(Math.PI * -0.5 * p);
    }

    replayContext.addMesh(mesh, false);
}

function createWall(frame) {
    const oyaId = frame.static.oya;
    const dice = frame.static.dice;
    const wall = frame.static.wall;
    const tilesDrawn = frame.tilesDrawn;
    const rinshanTilesDrawn = frame.rinshanTilesDrawn;
    const doraIndicators = frame.doraIndicators;

    const layoutOffset = -(17 * tileWidth + tileHeight + gap) / 2;
    const y = layoutOffset + 0.5 * tileHeight;
    const xStart = layoutOffset + 0.5 * tileWidth + tileHeight + gap;

    const diceSum = dice[0] + dice[1];
    const wallOffset = (20 - diceSum - oyaId) * 34 + diceSum * 2;

    var wallId = 0;

    for (let i = 0; i < 4; i++) {
        let x = xStart;
        for (let j = 0; j < 17; j++) {
            for (let k = 0; k < 2; k++) {
                const shiftedId = (wallId + wallOffset) % 136;
                if (shiftedId === 0) {
                    x += gap;
                }
                const rinshanId = shiftedId % 2 === 0 ? shiftedId + 1 : shiftedId - 1;
                if (rinshanId >= rinshanTilesDrawn && shiftedId < 136 - tilesDrawn) {
                    const tileId = wall[shiftedId];
                    const isDoraIndicator = shiftedId > 4 && shiftedId % 2 === 1 && shiftedId < 4 + doraIndicators * 2;
                    const placement = isDoraIndicator ? _tilePlacement.doraIndicator : _tilePlacement.wall;
                    addTile(i, tileId, x, y, k * tileDepth, placement);
                }
                wallId += 1;
                if (shiftedId === 13) {
                    x += gap;
                }
            }
            x += tileWidth;
        }
    }
}

function setStartingHands(frame) {
    frame.hands = [];
    const playerCount = frame.static.playerCount;
    for (let playerId = 0; playerId < playerCount; playerId++) {
        const tileIds = getDealtTileIds(frame.static.wall, playerId, frame.static.oya);
        frame.hands.push({ tiles: tileIds, melds: [], justCalled: false, drewRinshan: false });
    }
}

function getDealtTileIds(wall, playerId, oyaId) {
    var tileIds = [];
    const playerOffset = (playerId - oyaId  + 4) % 4;
    const offset = playerOffset * 4;
    for (let i = 0; i < 3; i++) {
        tileIds = tileIds.concat(wall.slice(136 - (offset + i * 16 + 4), 136 - (offset + i * 16)));
    }
    tileIds.push(wall[135 - (playerOffset + 3 * 4 * 4)]);
    intSort(tileIds);
    return tileIds;
}

function createMeld(playerId, x, meld) {
    const b = -(11 * tileWidth);
    const y = b - 0.5 * tileHeight;
    const tileIds = meld.tiles.slice(0);
    tileIds.sort((x1, x2) => x2 - x1);
    let isClosedKan = false;
    let tileCount = tileIds.length;
    if (meld.relativeFrom === 0) {
        isClosedKan = tileCount === 4;
    } else {
        remove(tileIds, meld.flipped);
        remove(tileIds, meld.added);
        if (meld.relativeFrom === 1) {
            tileIds.splice(0, 0, meld.flipped);
        } else if (meld.relativeFrom === 2) {
            tileIds.splice(1, 0, meld.flipped);
        } else if (meld.relativeFrom === 3) {
            tileIds.push(meld.flipped);
        }
    }

    tileCount = tileIds.length;
    for (let i = 0; i < tileCount; i++) {
        const tileId = tileIds[i];
        const isFaceDown = isClosedKan && (i === 0 || i === 3);
        const isFlipped = tileId === meld.flipped;
        const placement = isFlipped ? _tilePlacement.meldedFlipped : isFaceDown ? _tilePlacement.meldedFaceDown : _tilePlacement.melded;
        addTile(playerId, tileId, x, y, 0, placement);
        if (isFlipped && meld.added !== undefined) {
            addTile(playerId, tileId, x, y + tileWidth, 0, _tilePlacement.meldedFlipped);
        }
        x -= isFlipped ? tileHeight : tileWidth;
    }
    return x;
}

function addTile(playerId, tileId, x, y, z, placement) {
    const number = numberFromTileIdIncluding0(tileId);
    const suit = suitFromTileId(tileId);
    const mesh = replayContext.createTileMesh(number, suit);
    addTileMesh(mesh, playerId, x, y, z, placement);
}

function addGhostTile(playerId, tileId, x, y, z, placement) {
    const number = numberFromTileIdIncluding0(tileId);
    const suit = suitFromTileId(tileId);
    const mesh = replayContext.createGhostTileMesh(number, suit);
    addTileMesh(mesh, playerId, x, y, z, placement);
}

function addTileMesh(mesh, playerId, x, y, z, placement) {
    const p = getRotatedPlayerId(playerId);

    var open = 0;
    var flip = 0;
    if (placement === _tilePlacement.wall) {
        open = 2;
    } else if (placement === _tilePlacement.hand) {
        if (p === 0) {
            open = -0.4;
        } else {
            open = allHandsOpen ? 0 : 3;
        }
    }  else if (placement === _tilePlacement.pondFlipped) {
        flip = 1;
    }  else if (placement === _tilePlacement.pondGhostFlipped) {
        flip = 1;
    }  else if (placement === _tilePlacement.meldedFaceDown) {
        open = 2;
    } else if (placement === _tilePlacement.meldedFlipped) {
        flip = 1;
    }
    // else if (placement === _tilePlacement.melded)
    // else if (placement === _tilePlacement.pondGhost)
    // else if (placement === _tilePlacement.pond)
    // else if (placement === _tilePlacement.doraIndicator)

    if (flip % 2 === 1) {
        const a = (tileHeight - tileWidth) / 2;
        x -= a;
        y -= a;
    }
    
    mesh.rotateZ(Math.PI * 0.5 * p);
    mesh.translateX(x);
    mesh.translateY(y);
    mesh.translateZ(z);
    mesh.rotateY(Math.PI);
    mesh.rotateX(Math.PI * 0.5 * (open - 1));
    mesh.rotateY(Math.PI * 0.5 * flip);

    replayContext.addTile(mesh);
}

function suitFromTileId(tileId) {
    return Math.floor(tileId / (9 * 4));
}

function numberFromTileIdIncluding0(tileId) {
    if (tileId === 4 * 4 || tileId === 16 + 9 * 4 || tileId === 16 + 9 * 4 * 2)
        return 0;
    return 1 + Math.floor(tileId % (9 * 4) / 4);
}

function getRotatedPlayerId(playerId) {
    return (playerId + 4 - _observedPlayerId) % 4;
}

function remove(array, value) {
    const index = array.indexOf(value);
    if (index >= 0) {
        array.splice(index, 1);
    }
}

function removeMany(array, values) {
    const itemCount = values.length;
    for (let i = 0; i < itemCount; i++) {
        remove(array, values[i]);
    }
}

function intSort(array) {
    array.sort((a, b) => a - b);
}
