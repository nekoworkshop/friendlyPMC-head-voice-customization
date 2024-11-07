import { IDialogueChatBot } from "@spt/helpers/Dialogue/IDialogueChatBot";
import { NotificationSendHelper } from "@spt/helpers/NotificationSendHelper";
import { ISendMessageRequest } from "@spt/models/eft/dialog/ISendMessageRequest";
import { MemberCategory } from "@spt/models/enums/MemberCategory";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { MailSendService } from "@spt/services/MailSendService";
import { inject, injectable } from "tsyringe";

import { NotificationEventType } from "@spt/models/enums/NotificationEventType";
import { IWsChatMessageReceived } from "@spt/models/eft/ws/IWsChatMessageReceived";

import { IWsNotificationEvent } from "@spt/models/eft/ws/IWsNotificationEvent";
import { IGroupCharacter } from "@spt/models/eft/match/IGroupCharacter";

import { Message } from "@spt/models/eft/profile/ISptProfile";
import { MessageType } from "@spt/models/enums/MessageType";
import { HashUtil } from "@spt/utils/HashUtil";
import { ProfileHelper } from "@spt/helpers/ProfileHelper";

interface IWsGroupMatchInviteAccept extends IWsNotificationEvent, IGroupCharacter {}

@injectable()
export class KnightChatBot implements IDialogueChatBot {
	public constructor(@inject("PrimaryLogger") private logger: ILogger, @inject("MailSendService") private mailSendService: MailSendService, @inject("ProfileHelper") private profileHelper: ProfileHelper, @inject("NotificationSendHelper") private notificationSendHelper: NotificationSendHelper, @inject("HashUtil") private hashUtil: HashUtil) {
		this.logger = logger;
		this.mailSendService = mailSendService;
		this.notificationSendHelper = notificationSendHelper;
	}

	chatCommands = {
		joinRaid: ["Join me", "Party up", "Join", "Team up"],
		giveWeapon: ["Take ", "Use "],
	};

	isInGroup = false;

	getChatBot() {
		return {
			_id: "bossKnight",
			aid: 1113579,
			Info: {
				Level: 1,
				MemberCategory: MemberCategory.TRADER,
				SelectedMemberCategory: MemberCategory.TRADER,
				Nickname: "Knight",
				Side: "Usec",
			},
		};
	}

	public handleMessage(sessionId: string, request: ISendMessageRequest): string {
		if (request.text == "/help") {
			setTimeout(() => {
				this.mailSendService.sendUserMessageToPlayer(sessionId, this.getChatBot(), "Here is what I can do:");
				setTimeout(() => {
					let message = "I can join you in a raid, just say '" + this.chatCommands["joinRaid"].join("' or '") + "'.";
					message += "\n\nYou can give me a weapon kit to use, just say '" + this.chatCommands["giveWeapon"].join(" or ") + "' and the preset name.";
					this.mailSendService.sendUserMessageToPlayer(sessionId, this.getChatBot(), message);
				}, 1000);
			}, 1000);
			return request.dialogId;
		}
	}

	public acceptInvite(sessionId: string) {
		const profile = this.getChatBot();
		const dialog = this.notificationSendHelper["getDialog"](sessionId, MessageType.USER_MESSAGE, profile);

		const userProfile = this.profileHelper.getPmcProfile(sessionId);

		dialog.new += 1;
		const message: Message = {
			_id: this.hashUtil.generate(),
			uid: dialog._id,
			type: MessageType.USER_MESSAGE,
			dt: Math.round(Date.now() / 1000),
			text: `Right on! I'm ready for whatever!`,
			hasRewards: undefined,
			rewardCollected: undefined,
			items: undefined,
		};
		dialog.messages.push(message);

		this.isInGroup = true;

		const notification: IWsGroupMatchInviteAccept = {
			type: NotificationEventType.GROUP_MATCH_INVITE_ACCEPT,
			eventId: message._id,
			Info: Object.assign(profile.Info, {
				Level: userProfile.Info.Level,
			}),
			_id: profile._id,
			aid: profile.aid,
			isLeader: false,
			isReady: true,
		};
		this.notificationSendHelper.sendMessage(sessionId, notification);
	}
}
