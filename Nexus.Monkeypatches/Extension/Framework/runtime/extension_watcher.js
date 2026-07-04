import { evalInMainWorldAsync } from "../bridge/main_world";

export function watchExtension(extensionId) {
    // alert "watching extension ${extensionId}"
    evalInMainWorldAsync({
        source: `
        alert("watching extension ${extensionId}");
    ` });
    //...watch extension...
}

export function unwatchExtension(extensionId) {
    //...unwatch extension...
    evalInMainWorldAsync({
        source: `
        alert("unwatching extension ${extensionId}");
    ` });
}