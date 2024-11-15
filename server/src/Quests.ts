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
		itemLocation: ["RezervBase", "TarkovStreets"],
		itemId: "64e3b0f4e3b6f5a4a37e8c1d",
		itemContainer: {
			RezervBase: ["00975"],
			TarkovStreets: ["container_City_Design_Main_00064", "container_City_Design_Main_00019", "container_City_Design_Main_00062","container_City_Design_Main_00061"],
		},
	},
};

@injectable()
export class PitQuestItemEventRouter extends ItemEventRouterDefinition {
	mydb: { [key: string]: any } = {};
	mydbplace = "";

	private _questsWithItems: string[] = ["friendlypmc-knight-thieves"];

	constructor(@inject("EventOutputHolder") protected eventOutputHolder: EventOutputHolder, @inject("DatabaseService") protected databaseService: DatabaseService, @inject("RandomUtil") protected randomUtil: RandomUtil) {
		super();
	}

	public override getHandledRoutes(): HandledRoute[] {
		return [new HandledRoute("QuestAccept", false), new HandledRoute("QuestComplete", false)];
	}

	public override async handleItemEvent(eventAction: string, pmcData: IPmcData, body: any, sessionID: string): Promise<IItemEventRouterResponse> {
		if (eventAction == "QuestAccept") {
			const info: IAcceptQuestRequestData = body;

			this._questsWithItems.forEach(quest => {
				if (info.qid == quest) {
					this.mydb.progress = this.mydb.progress || {};
					let loc = this.randomUtil.getArrayValue(Quests[info.qid].itemLocation);
					this.mydb.progress["friendlypmc-knight-thieves"] = {
						itemLocation: loc,
						itemId: Quests["friendlypmc-knight-thieves"].itemId,
						itemContainer: this.randomUtil.getArrayValue(Quests[info.qid].itemContainer[loc]),
					};

					fs.writeFileSync(this.mydbplace + "progress.json", JSON.stringify(this.mydb.progress, null, 4));
				}
			});

			return {
				warnings: [],
				profileChanges: "",
			};
		}
	}

	public ModDB(db: { [key: string]: any }, path: string) {
		this.mydb = db;
		this.mydbplace = path;
	}

	public UpdateQuestProgress(pmc: IPmcData, raidConfig: { sameSideHostile: boolean; badGuy: boolean; englishBear: boolean; pmcArmbands: boolean; location: string }) {
		Object.keys(Quests).forEach(key => {
			const q = pmc.Quests.find(q => q.qid == key);
			if (q.status != 2) {
				if (this.mydb.progress) {
					delete this.mydb.progress[key];
				}
			} else if (this.mydb.progress && this.mydb.progress[key]) {
				if (this.mydb.progress[key].itemLocation) {
					const itemLocation = this.mydb.progress[key].itemLocation;
					const itemContainer = this.mydb.progress[key].itemContainer;
					const itemId = this.mydb.progress[key].itemId;
					if (raidConfig.location == itemLocation) {
						const mapData = this.databaseService.getLocation(itemLocation.toLowerCase());

						mapData.staticContainers.staticForced = [{ containerId: itemContainer, itemTpl: itemId }];
						mapData.staticContainers.staticContainers.forEach(container => {
							if (container.template.Id == itemContainer) {
								container.probability = 1;
							}
						});
					}
				}
			}
		});

		if (this.mydb.progress) fs.writeFileSync(this.mydbplace + "progress.json", JSON.stringify(this.mydb.progress, null, 4));
	}
}
