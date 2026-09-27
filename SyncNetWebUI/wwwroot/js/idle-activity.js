// Minimal JS interop for the idle-timeout feature (Services/IdleTimerService.cs) — Blazor WASM
// has no way to observe raw DOM activity (mousemove/keydown/etc.) from C# directly, so this
// bridges activity events back into .NET. All timing/threshold logic stays in C#; this file
// only pings .NET when the user does something, throttled so it isn't called on every single
// mousemove.
let lastPingAt = 0;
let activityHandler = null;
const THROTTLE_MS = 5000;
const EVENTS = ["mousemove", "mousedown", "keydown", "scroll", "touchstart"];

function startIdleActivityListener(dotNetRef) {
    stopIdleActivityListener();

    activityHandler = () => {
        const now = Date.now();
        if (now - lastPingAt < THROTTLE_MS) return;
        lastPingAt = now;
        dotNetRef.invokeMethodAsync("OnActivity");
    };

    EVENTS.forEach(evt => document.addEventListener(evt, activityHandler, { passive: true }));
}

function stopIdleActivityListener() {
    if (!activityHandler) return;
    EVENTS.forEach(evt => document.removeEventListener(evt, activityHandler));
    activityHandler = null;
}
