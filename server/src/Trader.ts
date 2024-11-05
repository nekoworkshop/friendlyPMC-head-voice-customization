import { TraderHelper as Helper } from "./TraderHelper";

import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { ITraderConfig } from "@spt/models/spt/config/ITraderConfig";
import { Traders } from "@spt/models/enums/Traders";
import { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { JsonUtil } from "@spt/utils/JsonUtil";
import { ITraderAssort, ITraderBase } from "@spt/models/eft/common/tables/ITrader";

import traderBase = require("../traderKnight/base.json");
import traderAssort = require("../traderKnight/assort.json");
import traderQuests = require("../traderKnight/quests.json");

export class KnightTrader {
	traderHelper: Helper;

	constructor(modName: string, preSptModLoader: PreSptModLoader, imageRouter: ImageRouter, private traderConfig: ITraderConfig, private ragfairConfig: IRagfairConfig, private jsonUtil: JsonUtil) {
		this.traderHelper = new Helper();

		this.traderHelper.registerProfileImage(traderBase, modName, preSptModLoader, imageRouter);
		this.traderHelper.setTraderUpdateTime(this.traderConfig, traderBase, 3600, 7200);

		Traders[traderBase._id] = traderBase._id;
		this.ragfairConfig.traders[traderBase._id] = false;
	}

	AddToDb(tables: IDatabaseTables) {
		tables.traders[traderBase._id] = {
			assort: this.jsonUtil.deserialize(this.jsonUtil.serialize(traderAssort)) as ITraderAssort, // assorts are the 'offers' trader sells, can be a single item (e.g. carton of milk) or multiple items as a collection (e.g. a gun)
			base: this.jsonUtil.deserialize(this.jsonUtil.serialize(traderBase)) as ITraderBase, // Deserialise/serialise creates a copy of the json and allows us to cast it as an ITraderBase
			questassort: this.jsonUtil.deserialize(this.jsonUtil.serialize(traderQuests)), // questassort is empty as trader has no assorts unlocked by quests
		};

		this.traderHelper.addTraderToLocales(traderBase, tables, traderBase.name, traderBase._id, traderBase.nickname, traderBase.location, "Commander of the Goons squad");
	}
}
