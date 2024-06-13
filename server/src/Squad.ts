import { DependencyContainer, inject } from "tsyringe";
import { DatabaseServer } from "@spt-aki/servers/DatabaseServer";

import { BotDifficultyHelper } from "@spt-aki/helpers/BotDifficultyHelper";
import { BotController } from "@spt-aki/controllers/BotController";

import { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";
import { IPmcConfig } from "@spt-aki/models/spt/config/IPmcConfig";

import { ILogger } from "@spt-aki/models/spt/utils/ILogger";

import { Difficulty } from "@spt-aki/models/eft/common/tables/IBotType";

import { MatchCallbacks } from "@spt-aki/callbacks/MatchCallbacks";
import { LogTextColor } from "@spt-aki/models/spt/logging/LogTextColor";

import { ILocations } from "@spt-aki/models/spt/server/ILocations";

import { openZonesMap } from "./AOZExports";

class friendlyPMC {
	config = {
		sameSideHostile: false,
		armbands: true,
	};

	Logger: ILogger;
	Bots: IBotConfig;

	originalgetPmcDifficultySettings: BotDifficultyHelper["getPmcDifficultySettings"];
	originalgetBotDifficulty: BotController["getBotDifficulty"];

	originalGetRaidConfiguration: MatchCallbacks["getRaidConfiguration"];

	preAkiLoad(container: DependencyContainer) {
		this.Logger = container.resolve("WinstonLogger");

		try {
			this.config = Object.assign(this.config, require("../config.json"));
		} catch (e) {
			this.Logger.error("friendlyPMC: something is wrong with the config, check below\n");
			console.error(e);
		}
		// patch getPmcDifficultySettings as that is where we actually make the bots be friendly
		this.getPmcDifficultySettings = this.getPmcDifficultySettings.bind(this);
		container.afterResolution(
			"BotDifficultyHelper",
			(_t, result: BotDifficultyHelper) => {
				if (!this.originalgetPmcDifficultySettings) {
					this.originalgetPmcDifficultySettings = result.getPmcDifficultySettings.bind(result);
				}

				result.getPmcDifficultySettings = this.getPmcDifficultySettings;
			},
			{ frequency: "Always" }
		);
		// patch getBotDifficulty as that is where we actually make the bots be friendly
		this.getBotDifficulty = this.getBotDifficulty.bind(this);
		container.afterResolution(
			"BotController",
			(_t, result: BotController) => {
				if (!this.originalgetBotDifficulty) {
					this.originalgetBotDifficulty = result.getBotDifficulty.bind(result);
				}
				result.getBotDifficulty = this.getBotDifficulty;
			},
			{ frequency: "Always" }
		);
	}

	postDBLoad(container: DependencyContainer) {
		const configServer: any = container.resolve("ConfigServer");
		const Bots: IBotConfig = configServer.getConfig("aki-bot");
		const PMCBOT: IPmcConfig = configServer.getConfig("aki-pmc");

		const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
		const tables = databaseServer.getTables();

		const globals = tables.globals;

		// same side hostile is being changed elsewhere - do this to avoid unwanted outcome
		PMCBOT.chanceSameSideIsHostilePercent = -1;
		// this is what actually makes bots follow you
		for (let lvl in globals.config.FenceSettings.Levels) {
			globals.config.FenceSettings.Levels[lvl].BotFollowChance = 100;
			globals.config.FenceSettings.Levels[lvl].ScavAttackSupport = true;
			globals.config.FenceSettings.Levels[lvl].BotApplySilenceChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotGetInCoverChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotHelpChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotSpreadoutChance = 100;
			globals.config.FenceSettings.Levels[lvl].BotStopChance = 100;
			// stop spt* bosses from attacking you
			globals.config.FenceSettings.Levels[lvl].HostileBosses = false;
		}

		this.Bots = Bots;

		if (this.config.armbands) {
			this.Logger.logWithColor("friendlyPMC: Adding Armbands to bots...", LogTextColor.BLUE);

			const armbandColors: Record<string, string> = {
				blue: "5b3f3af486f774679e752c1f",
				green: "5b3f3b0186f774021a2afef7",
				red: "5b3f3ade86f7746b6b790d8e",
				white: "5b3f16c486f7747c327f55f7",
				yellow: "5b3f3b0e86f7746752107cda",
				purple: "5f9949d869e2777a0e779ba5",
			};

			for (const botType in tables.bots.types) {
				const bot = tables.bots.types[botType];
				const equipmentArmband: Record<string, number> = {};
				switch (botType) {
					case "assaultgroup":
					case "usec":
						bot.chances.equipment.ArmBand = 100;
						equipmentArmband[armbandColors.blue] = 1;
						bot.inventory.equipment.ArmBand = equipmentArmband;
						break;
					case "bear":
						bot.chances.equipment.ArmBand = 100;
						equipmentArmband[armbandColors.red] = 1;
						bot.inventory.equipment.ArmBand = equipmentArmband;
						break;
				}
			}
		}

		// open all zones to the bots
		const locations: ILocations = tables.locations;
		for (const altLocation in openZonesMap) {
			locations[altLocation].base.OpenZones = openZonesMap[altLocation].join(",");
			this.Logger.info(`Opened ${locations[altLocation].base.OpenZones} for bots in ${locations[altLocation].base.Name} location`);
		}
	}

	private _makeFriendlyOrHostile(diff: Difficulty, pmcType: string) {
		const clearWrongEnemy = (mind: Record<string, string | number | boolean | string[]>, type: string) => {
			const enemyList = <string[]>mind.ENEMY_BOT_TYPES;

			const idx = enemyList.indexOf(type);
			if (idx > -1) enemyList.splice(idx, 1);

			const idxl = enemyList.indexOf(type.toLowerCase());
			if (idxl > -1) enemyList.splice(idxl, 1);
		};

		let is_hostile = this.config.sameSideHostile || false;

		// force the friendly mind here as some mods may overwrite things
		if (pmcType == "bear" || pmcType == "usec") {
			Object.assign(diff.Mind, {
				DEFAULT_ENEMY_BEAR: pmcType == "usec" || is_hostile,
				DEFAULT_ENEMY_SAVAGE: true,
				DEFAULT_ENEMY_USEC: pmcType == "bear" || is_hostile,
				DEFAULT_BEAR_BEHAVIOUR: !is_hostile && pmcType == "bear" ? "Ignore" : "Attack",
				DEFAULT_SAVAGE_BEHAVIOUR: "Attack",
				DEFAULT_USEC_BEHAVIOUR: !is_hostile && pmcType == "usec" ? "Ignore" : "Attack",
				CAN_RECIVE_PLAYER_REQUESTS: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS_USEC: !is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS_BEAR: !is_hostile,
			});

			const Core: { [key: string]: any } = {};
			Core.MAX_COME_WITH_ME_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_BASE_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_HOLD_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_GO_TO_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_GET_IN_COVER_REQUESTS_PER_PLAYER = 9999;
			Core.MAX_WAIT_REQUESTS_PER_PLAYER = 9999;
			Core.START_ACTIVE_FOLLOW_PLAYER_EVENT = true;
			Core.GESTUS_MAX_ANSWERS = 9999;
			//Core.GESTUS_REQUEST_LIFETIME = 50;
			Core.GESTUS_ANYWAY_CHANCE = 0;
			Core.START_DIST_TO_COV = 20000.0;
			Core.MAX_DIST_TO_COV = 20000.0;
			Core.MAX_REQUESTS__PER_GROUP = 9999;
			Core.MAX_REQUESTS_PER_GROUP = 9999;

			Object.assign(diff.Core, Core);
			Object.assign(diff.Mind, Core, {
				MAX_START_AGGRESION_COEF: 9999,
				MIN_START_AGGRESION_COEF: 9999,
				FRIEND_AGR_KILL: 0.000001,
				FRIEND_DEAD_AGR_LOW: -0.000001,
			});

			if (!is_hostile) {
				// do these do anything?

				if (pmcType == "bear") {
					clearWrongEnemy(diff.Mind, "sptBear");
					clearWrongEnemy(diff.Mind, "bear");
				} else if (pmcType == "usec") {
					clearWrongEnemy(diff.Mind, "sptUsec");
					clearWrongEnemy(diff.Mind, "usec");
				}
			}
			// ensure these settings are set last as they are not dependent of "is_hostile" flag
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: is_hostile,
				CAN_RECEIVE_PLAYER_REQUESTS_SAVAGE: false,
			});
		}

		if (pmcType == "exusec" || pmcType == "rogue") {
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: false,
				REVENGE_BOT_TYPES: [],
				DEFAULT_USEC_BEHAVIOUR: "Attack",
			});
		}

		if (pmcType == "gifter") {
			Object.assign(diff.Mind, {
				ENEMY_BY_GROUPS_PMC_PLAYERS: false,
				REVENGE_BOT_TYPES: [],
				DEFAULT_USEC_BEHAVIOUR: "Ignore",
				DEFAULT_BEAR_BEHAVIOUR: "Ignore",
				DEFAULT_ENEMY_BEAR: false,
				DEFAULT_ENEMY_USEC: false,
			});
		}

		return diff;
	}

	/** Overwrite get difficulity method to patch the friendly/hostile settings */
	getPmcDifficultySettings(pmcType: "bear" | "usec", difficulty: string, usecType: string, bearType: string): any {
		const result = this.originalgetPmcDifficultySettings(pmcType, difficulty, usecType, bearType);

		return this._makeFriendlyOrHostile(result, pmcType);
	}

	/** Overwrite get difficulity method to patch the friendly/hostile settings */
	getBotDifficulty(type: string, difficulty: string): any {
		let result = this.originalgetBotDifficulty(type, difficulty);

		return this._makeFriendlyOrHostile(result, type);
	}
}

export const mod = new friendlyPMC();
