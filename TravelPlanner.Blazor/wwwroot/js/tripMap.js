export function create(element, receiver, options) {
    if (!window.L) throw new Error('Карта недоступна');
    const map = L.map(element).setView([55.75, 37.62], 4);
    const tiles = L.tileLayer(options.tileUrl, {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);
    tiles.on('tileerror', () => receiver.invokeMethodAsync('MapUnavailable').catch(() => {}));
    const markers = L.layerGroup().addTo(map);
    let selected;
    map.on('click', event => {
        const latitude = Math.max(-90, Math.min(90, event.latlng.lat));
        const longitude = ((event.latlng.lng + 180) % 360 + 360) % 360 - 180;
        if (selected) map.removeLayer(selected);
        selected = L.circleMarker([latitude, longitude], {radius: 9, color:'#943B27', fillColor:'#B64D34', fillOpacity:.65}).addTo(map);
        receiver.invokeMethodAsync('SelectPoint', latitude, longitude).catch(() => {});
    });
    element.tripMap = { map, markers, selected: () => selected };
    return true;
}

export function show(element, places, fit) {
    const state = element.tripMap;
    if (!state) return;
    state.markers.clearLayers();
    for (const place of places) {
        const content = document.createElement('div');
        const title = document.createElement('strong');
        title.textContent = place.title;
        const note = document.createElement('p');
        note.textContent = place.note;
        content.append(title, note);
        L.circleMarker([place.latitude, place.longitude], {radius:8,color:'#943B27',fillColor:'#B64D34',fillOpacity:.85})
            .bindPopup(content).addTo(state.markers);
    }
    if (fit && places.length) state.map.fitBounds(places.map(p => [p.latitude,p.longitude]), {padding:[32,32],maxZoom:14});
    state.map.invalidateSize();
}

export function focus(element, latitude, longitude) {
    element.tripMap?.map.setView([latitude, longitude], 15);
}

export function dispose(element) {
    element.tripMap?.map.remove();
    delete element.tripMap;
}
