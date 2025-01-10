export function isFunction(value: any): boolean {
	return typeof value === "function";
}

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

/**
 * Executes a provided function once for each of the object values
 * @param {Object.} object
 * @param {Function} iterator - function to execute for each of the values of `object`
 */
export function objectForEach<T extends { [key: string]: any }, K extends keyof T>(obj: T, iterator: (value: T[K], index: string) => boolean | void, thisArg = obj) {
	var _this = thisArg || obj,
		next: boolean;

	if (!isObject(obj)) throw new TypeError("Object.forEach called on non-object. The given value is " + typeof obj);
	else if (!isFunction(iterator)) throw new TypeError("The given iterator is not a function");
	else {
		for (var prop in obj) {
			//@ts-ignore
			if ("hasOwn" in Object) {
				//@ts-ignore
				if (!Object["hasOwn"](obj, prop)) continue;
			} else if (obj.hasOwnProperty && !obj.hasOwnProperty(prop)) continue;

			next = iterator.apply(_this, [obj[prop], prop]);
			if (typeof next == "boolean" && !next) {
				break;
			}
		}
	}
}
