﻿// urlParams:
// v: selected partial view. blame if undefined.
// r: replayId for blame
// g: gameId for blame
// f: frameId for blame
// p: playerId for blame

function onPopstate(e) {
    const urlParams = e.state;
    showView(urlParams && urlParams.v);
    replayOnPopState(urlParams);
}

// displays the partial view with the given contentName. blame if undefined. Also updates browser history.
function navigateTo(contentName) {
    showView(contentName);
    if (contentName === "blame") {
        const replayParams = getUrlParamsFromInputs();
        setBrowserHistory(replayParams);
    } else {
        setBrowserHistory({ v: contentName });
    }
}

function updateHistory(urlParams) {
    if (urlParams && urlParams.r) {
        updateUrlParams(urlParams);
    } else {
        updateUrlParams({ r: undefined, p: undefined, g: undefined, f: undefined });
    }
}

function onReplayIdChanged(replayId) {
    const d = { r: replayId, p: 0, g: 0, f: 0 };
    loadReplay(d);
}

function getReplayId() {
    return getValueFromComboBox("#replayId");
}

function getUrlParamsFromInputs() {
    const replayId = getReplayId();
    const playerId = getIntFromInput("#playerId");
    const game = getIntFromInput("#gameId");
    const frame = getIntFromInput("#frameId");
    return { r: replayId, p: playerId, g: game, f: frame };
}

function setFrameInputData(urlParams) {
    setValueToInput("#replayId", urlParams.r);
    setValueToInput("#playerId", urlParams.p);
    setValueToInput("#gameId", urlParams.g);
    setValueToInput("#frameId", urlParams.f);
}

function replayOnPopState(state) {
    if (state) {
        const oldReplayId = getReplayId();
        setFrameInputData(state);
        const newReplayId = getReplayId();
        if (oldReplayId !== newReplayId) {
            loadReplay(getUrlParamsFromInputs());
        }

        replayContext.createTiles(() => arrange());
    }
}

function onFrameChanged() {
    const urlParams = getUrlParamsFromInputs();
    updateHistory(urlParams);
    replayContext.createTiles(() => arrange());
}

function getIntFromParams(params, key) {
    return parseInt(params.get(key)) || 0;
}

function getStringFromParams(params, key) {
    if (!params.has(key))
        return undefined;
    return params.get(key);
}

function getUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const view = getIntFromParams(params, "v");
    const replayId = getStringFromParams(params, "r");
    const playerId = getIntFromParams(params, "p");
    const game = getIntFromParams(params, "g");
    const frame = getIntFromParams(params, "f");
    return {v: view, r: replayId, p: playerId, g: game, f: frame };
}

function getBrowserHistoryData() {
    return window.history.state;
}

// data must be an object with property names matching the keys used in the url parameters
function setBrowserHistory(data) {
    const previousData = getBrowserHistoryData();
    const previousParams = _getHistoryParameterString(previousData);
    const params = _getHistoryParameterString(data);
    if (previousParams === params) {
        return;
    }
    const url = `//${location.host}${location.pathname}${params}`;
    window.history.pushState(data, "", url);
}

function _getHistoryParameterString(data) {
    if (data) {
        const keys = Object.keys(data).filter(k => data[k]);
        if (keys.length === 0) {
            return "";
        }
        keys.sort();
        const x = keys.map(k => k + "=" + data[k]).join("&");
        return `?${x}`;
    } else {
        return "";
    }
}

// keeps existing parameters, adds new ones and overwrites parameters with identical keys
// keys with undefined values will be removed
function updateUrlParams(data) {
    const current = Object.assign({}, getBrowserHistoryData());
    const keys = Object.keys(data);
    const count = keys.length;
    for (let i = 0; i < count; i++) {
        const key = keys[i];
        const value = data[key];
        if (value) {
            current[key] = value;
        } else {
            delete current[key];
        }
    }
    setBrowserHistory(current);
}

function getIntFromInput(id) {
    const input = document.querySelector(id);
    return input.value ? parseInt(input.value) : 0;
}

function setValueToInput(id, value) {
    const x = value || "";
    document.querySelector(id).value = x;
}

function getValueFromComboBox(id) {
    return document.querySelector(id).value;
}

// displays the partial view with the given contentName. blame if undefined.
function showView(target) {
    const contentName = target || "blame";
    const contents = document.getElementsByClassName("content");
    const count = contents.length;
    for (let i = 0; i < count; i++) {
        const content = contents[i];
        content.hidden = content.dataset.content !== contentName;
    }
}