import { HandledRoute, ItemEventRouterDefinition } from "@spt/di/Router";
import { IPmcData } from "@spt/models/eft/common/IPmcData";
import { IItemEventRouterResponse } from "@spt/models/eft/itemEvent/IItemEventRouterResponse";
import { DatabaseService } from "@spt/services/DatabaseService";
import { inject, injectable } from "tsyringe";

import fs from "fs";
import { EventOutputHolder } from "@spt/routers/EventOutputHolder";
import { IAcceptQuestRequestData } from "@spt/models/eft/quests/IAcceptQuestRequestData";
import { RandomUtil } from "@spt/utils/RandomUtil";

export const Quests = {
	"friendlypmc-knight-thieves": {
		itemLocation: ["TarkovStreets"],
		itemId: "64e3b0f4e3b6f5a4a37e8c1d",
		itemContainer: {
			TarkovStreets: ["container_City_Design_Main_00017", "container_City_Design_Main_00020", "container_City_Design_Main_00022"],
		},
		itemCondition: "friendlypmc-knight-thieves-4",
	},
};

@injectable()
export class PitQuestItemEventRouter extends ItemEventRouterDefinition {
	mydb: { [key: string]: any } = {};
	mydbplace = "";

	constructor(@inject("EventOutputHolder") protected eventOutputHolder: EventOutputHolder, @inject("DatabaseService") protected databaseService: DatabaseService, @inject("RandomUtil") protected randomUtil: RandomUtil) {
		super();
	}

	public override getHandledRoutes(): HandledRoute[] {
		return [new HandledRoute("QuestAccept", false), new HandledRoute("QuestComplete", false)];
	}

	public override async handleItemEvent(eventAction: string, pmcData: IPmcData, body: any, sessionID: string): Promise<IItemEventRouterResponse> {
		if (eventAction == "QuestAccept") {
			const info: IAcceptQuestRequestData = body;

			// do updates on quests with dynamic objectives
			Object.keys(Quests).forEach(key => {
				let q = Quests[key];
				if (info.qid == key) {
					this.mydb.progress = this.mydb.progress || {};
					this.mydb.progress[sessionID] = this.mydb.progress[sessionID] || {};
					const userProgress = this.mydb.progress[sessionID];
					// - if quest is an item quest, decide where to put the item
					if (q.itemLocation) {
						let loc: string = this.randomUtil.getArrayValue(q.itemLocation);
						userProgress[key] = {
							itemLocation: loc,
							itemId: q.itemId,
							itemContainer: this.randomUtil.getArrayValue(<string[]>q.itemContainer[loc]),
						};

						// -- check if user already has the item
						if (!pmcData.Inventory.items.find(item => item._tpl == q.itemId)) {
							// -- update location container to spawn item
							let mapData = this.databaseService.getLocation(loc.toLowerCase());
							if (mapData) {
								mapData.staticContainers.staticForced = mapData.staticContainers.staticForced || [];
								mapData.staticContainers.staticForced.push({ containerId: userProgress[key].itemContainer, itemTpl: userProgress[key].itemId });
								mapData.staticContainers.staticContainers.forEach(container => {
									if (container.template.Id == userProgress[key].itemContainer) {
										container.probability = 1;
									}
								});
							}
						}
					}
				}
			});

			// - save progress
			if (this.mydb.progress) fs.writeFileSync(this.mydbplace + "progress.json", JSON.stringify(this.mydb.progress, null, 4));
		} else if (eventAction == "QuestComplete") {
			if (!this.mydb?.progress || !this.mydb.progress[sessionID]) return;

			const info: IAcceptQuestRequestData = body;
			for (let k in this.mydb.progress[sessionID]) {
				let q = this.mydb.progress[sessionID][k];
				if (k == info.qid) {
					if (q.itemLocation) {
						let mapData = this.databaseService.getLocation(q.itemLocation.toLowerCase());
						if (mapData) {
							mapData.staticContainers.staticForced = mapData.staticContainers.staticForced || [];
							mapData.staticContainers.staticForced = mapData.staticContainers.staticForced.filter(container => container.itemTpl != q.itemId);
						}
					}
					delete this.mydb.progress[k];
					break;
				}
			}

			if (this.mydb.progress) fs.writeFileSync(this.mydbplace + "progress.json", JSON.stringify(this.mydb.progress, null, 4));
		}

		return {
			warnings: [],
			profileChanges: "",
		};
	}

	public ModDB(db: { [key: string]: any }, path: string) {
		this.mydb = db;
		this.mydbplace = path;
	}
}
