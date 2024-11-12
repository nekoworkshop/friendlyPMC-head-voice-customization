export function isObject(value: any): value is { [key: string]: any } {
	return value !== null && typeof value === "object" && !isArray(value);
}

export function isArray(arg: any): arg is Array<any> {
	return Array.isArray(arg);
}

function _clone<T extends Array<any> | Object>(obj: T): T {
	if (!isObject(obj) && !isArray(obj)) return obj;

	let isJson = isArray(obj) ? false : true;

	let target: any = !isJson ? [] : {};

	let i = 0,
		ln = 0;

	if (isJson) {
		let keys = Object.keys(obj);
		for (ln = keys.length; i < ln; i++) {
			let k = keys[i];
			target[k] = _clone(obj[k]);
		}
	} else {
		for (ln = (<any[]>obj).length; i < ln; i++) {
			target[i] = _clone(obj[i]);
		}
	}

	return target;
}

export function objectCopy<T>(source: T) {
	if (!isObject(source) && !isArray(source)) throw new TypeError("Object.copy called on non-object. The given value is " + typeof source);
	return _clone(source);
}
