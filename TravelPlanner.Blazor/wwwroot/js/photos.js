export function create(bytes, contentType) {
    return URL.createObjectURL(new Blob([bytes], {type: contentType}));
}
export function revoke(url) { URL.revokeObjectURL(url); }
