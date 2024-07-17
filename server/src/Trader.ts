import { TraderServiceType } from "@spt/models/enums/TraderServiceType";
import { ITraderConfig } from "@spt/models/spt/config/ITraderConfig";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";

export function SetFreemanTrader(Tables: IDatabaseTables, Traders: ITraderConfig) {
	const traders = Tables.traders;

	const trdid = "friendlypmc-return-loot";

	// add special trader for delivering items back
	traders[trdid] = {
		//@ts-ignore
		assort: {
			barter_scheme: {},
			items: [],
			loyal_level_items: {},
		},
		base: {
			_id: trdid,
			availableInRaid: true,
			avatar: "/files/trader/avatar/general.jpg",
			balance_dol: 0,
			balance_eur: 0,
			balance_rub: 7000000,
			buyer_up: false,
			currency: "RUB",
			customization_seller: false,
			discount: 0,
			discount_end: 0,
			gridHeight: 120,
			insurance: {
				availability: false,
				excluded_category: [],
				max_return_hour: 0,
				max_storage_time: 48,
				min_payment: 0,
				min_return_hour: 0,
			},
			items_buy: {
				category: [],
				id_list: [],
			},
			items_buy_prohibited: {
				category: [null],
				id_list: ["64d0b40fbe2eed70e254e2d4"],
			},
			location: "БТР",
			loyaltyLevels: [
				{
					buy_price_coef: 0,
					exchange_price_coef: 0,
					heal_price_coef: 0,
					insurance_price_coef: 0,
					minLevel: 1,
					minSalesSum: 0,
					minStanding: 0,
					repair_price_coef: 0,
				},
			],
			medic: false,
			name: "Alex Freeman",
			nextResupply: 1703691958,
			nickname: "sarge",
			repair: {
				availability: false,
				currency: "5449016a4bdc2d6f028b456f",
				currency_coefficient: 1,
				excluded_category: [],
				excluded_id_list: [],
				quality: 0,
			},
			sell_category: [],
			//@ts-ignore
			sell_modifier_for_prohibited_items: 0,
			surname: "Freeman",
			unlockedByDefault: false,
		},
		dialogue: {
			itemsDelivered: ["657399489b19e826a721d75c 0", "657399489b19e826a721d75c 1", "657399489b19e826a721d75c 2"],
		},
		questassort: {
			started: {},
			success: {},
			fail: {},
		},
		services: [
			{
				serviceType: TraderServiceType.BTR_ITEMS_DELIVERY,
			},
		],
	};

	Traders.updateTime.push({
		//@ts-ignore
		_name: "FRIENDLYPMC",
		traderId: trdid,
		seconds: {
			min: 3000,
			max: 7500,
		},
	});

	const locales = Object.values(Tables.locales.global) as Record<string, string>[];
	for (const locale of locales) {
		locale[`${trdid} FullName`] = "Alex Freeman";
		locale[`${trdid} FirstName`] = "Alex";
		locale[`${trdid} Nickname`] = "Sarge";
		locale[`${trdid} Location`] = "БТР";
		locale[`${trdid} Description`] = "";
	}
}
