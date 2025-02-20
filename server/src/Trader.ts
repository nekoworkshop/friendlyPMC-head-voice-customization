import { TraderHelper as Helper } from "./TraderHelper";

import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { ITraderConfig } from "@spt/models/spt/config/ITraderConfig";
import { Traders } from "@spt/models/enums/Traders";
import { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { JsonUtil } from "@spt/utils/JsonUtil";
import { ITraderAssort, ITraderBase } from "@spt/models/eft/common/tables/ITrader";

import fs from "fs";
import path from "path";

export abstract class TraderBase {
	traderHelper: Helper;

	abstract traderDescription: string;

	private traderFiles: {
		base: string;
		assort: string;
		quests: string;
		weapons?: string;
	};

	public traderBase: ITraderBase;
	private traderAssort: ITraderAssort;
	private traderQuests: Record<string, Record<string, string>>;

	constructor(
		modName: string,
		trader: {
			base: string;
			assort: string;
			quests: string;
			weapons?: string;
		},
		preSptModLoader: PreSptModLoader,
		imageRouter: ImageRouter,
		private traderConfig: ITraderConfig,
		private ragfairConfig: IRagfairConfig,
		private jsonUtil: JsonUtil
	) {
		this.traderHelper = new Helper();
		this.traderFiles = trader;
		this.OnConstruct(modName, preSptModLoader, imageRouter);
	}

	protected OnConstruct(modName: string, preSptModLoader: PreSptModLoader, imageRouter: ImageRouter) {
		this.traderBase = require(this.traderFiles.base);
		this.traderAssort = require(this.traderFiles.assort);
		this.traderQuests = require(this.traderFiles.quests);

		this.traderHelper.registerProfileImage(this.traderBase, modName, preSptModLoader, imageRouter);
		this.traderHelper.setTraderUpdateTime(this.traderConfig, this.traderBase, 3600, 7200);

		Traders[this.traderBase._id] = this.traderBase._id;
		this.ragfairConfig.traders[this.traderBase._id] = false;
		// if trader has weapons, read the "weapons" dir and load each json file

		const weaponsPath = this.traderFiles.weapons ? path.join(__dirname, this.traderFiles.weapons) : null;
		if (weaponsPath && fs.existsSync(weaponsPath)) {
			const files = fs.readdirSync(weaponsPath);
			for (const file of files) {
				try {
					const weapon: {
						price: {
							_tpl: string;
							count: number;
						};
						level: number;
						item: { [key: string]: any }[];
					} = require(`${weaponsPath}/${file}`);
					const wpn = weapon.item[0];
					this.traderAssort.barter_scheme[wpn._id] = [[weapon.price]];
					this.traderAssort.loyal_level_items[wpn._id] = weapon.level;
					this.traderAssort.items.push.apply(this.traderAssort.items, weapon.item);
				} catch (e) {
					console.error(`Failed to load weapon: ${file}`);
					console.error(e);
				}
			}
		}
	}

	AddToDb(tables: IDatabaseTables) {
		tables.traders[this.traderBase._id] = {
			assort: this.traderAssort as ITraderAssort, // assorts are the 'offers' trader sells, can be a single item (e.g. carton of milk) or multiple items as a collection (e.g. a gun)
			base: this.traderBase as ITraderBase, // Deserialise/serialise creates a copy of the json and allows us to cast it as an ITraderBase
			questassort: this.traderQuests, // questassort is empty as trader has no assorts unlocked by quests
		};

		this.traderHelper.addTraderToLocales(this.traderBase, tables, this.traderBase.name, this.traderBase._id, this.traderBase.nickname, this.traderBase.location, this.traderDescription);
	}
}

export class KnightTrader extends TraderBase {
	traderDescription = "Commander of the Goons squad";

	constructor(modName: string, preSptModLoader: PreSptModLoader, imageRouter: ImageRouter, traderConfig: ITraderConfig, ragfairConfig: IRagfairConfig, jsonUtil: JsonUtil) {
		super(
			modName,
			{
				base: "../traderKnight/base.json",
				assort: "../traderKnight/assort.json",
				quests: "../traderKnight/quests.json",
				weapons: "../traderKnight/weapons",
			},
			preSptModLoader,
			imageRouter,
			traderConfig,
			ragfairConfig,
			jsonUtil
		);
	}
}

export class GeneralTrader extends TraderBase {
	traderDescription = "General Jonas Creed";
	constructor(modName: string, preSptModLoader: PreSptModLoader, imageRouter: ImageRouter, traderConfig: ITraderConfig, ragfairConfig: IRagfairConfig, jsonUtil: JsonUtil) {
		super(
			modName,
			{
				base: "../traderGeneral/base.json",
				assort: "../traderGeneral/assort.json",
				quests: "../traderGeneral/quests.json",
			},
			preSptModLoader,
			imageRouter,
			traderConfig,
			ragfairConfig,
			jsonUtil
		);
	}
}
