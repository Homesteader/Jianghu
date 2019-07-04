﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TenGameView : BaseView
{
    #region ui

    public Transform mContent;//content

    public TenPlayerUI[] mAllPlayers;//所有玩家

    public TenSelfPlayer mSelfPlayer;//自己

    public TenBasePlayerInfo mPlayerBaseItem;//玩家的基础信息节点

    public UILabel mRoomId;//房间号

    public UILabel mBaseScoreLabel;//底分

    public UILabel mTotalWinLabel;//总输赢的分数

    public GameObject mLastTimeGo;//倒计时

    public GameObject mFlyZhuang;//庄家的飞

    public UILabel mTimeLabel;//时间显示

    private TenCoinFlyWidget mNiuniuCoinFlyWidget;//飞金币和显示输赢积分动画

    private Dictionary<int, TenPlayerUI> mPlayerDic = new Dictionary<int, TenPlayerUI>();//玩家座位号对应ui


    private int mLastTime = 0;//倒计时

    private string mLastTimeContent;//提示信息

    private Vector3 mFlyZhuangPosition;//庄家飞的原始位置

    private bool mRandomZhuangDown = true;//随机庄是否结束

    private bool mCastDown = true;//发牌是否结束

    #endregion


    #region Unity 函数


    protected override void Start()
    {
        base.Start();
        mFlyZhuangPosition = mLastTimeGo.transform.position;

        //
        Debug.Log("进入tengameview");
    }

    protected override void Update()
    {
        mTimeLabel.text = string.Format("{0:D2}:{1:D2}:{2:D2}", System.DateTime.Now.Hour, System.DateTime.Now.Minute, System.DateTime.Now.Second);
    }

    #endregion


    #region 游戏逻辑

    /// <summary>
    /// 清理桌面
    /// </summary>
    public void CleanDesk()
    {
        List<int> list = new List<int>();
        list.AddRange(mPlayerDic.Keys);
        for (int i = 0; i < list.Count; i++)
        {
            mPlayerDic[list[i]].CleanHandCards();
            mPlayerDic[list[i]].SetQiangZhuangResult(false, 0);
            mPlayerDic[list[i]].SetCardType(false, 0, 0);
            mPlayerDic[list[i]].SetZhuangState(false);
            mPlayerDic[list[i]].UpdateCathecticCoin("");
        }

        BaseViewWidget.CloseWidget<TenCuoCardWidget>();
        mSelfPlayer.CloseCuoCardWidget();

        mRandomZhuangDown = true;
        mCastDown = true;
        //
        mSelfPlayer.HidePlayerPoint();
    }

    /// <summary>
    /// 清除桌面所有数据
    /// </summary>
    public void CleanAllDesk()
    {
        CleanDesk();
        List<int> list = new List<int>();
        list.AddRange(mPlayerDic.Keys);
        for (int i = 0; i < list.Count; i++)
        {
            mPlayerDic[list[i]].CleanHandCards();
            mPlayerDic[list[i]].CleanPlayer();
        }
        mSelfPlayer.SetBetBtnItemState(false);
        mSelfPlayer.HideLiangCuoState();
        ShowLastTime("", 0);
        mPlayerDic.Clear();
        mSelfPlayer.HidePlayerPoint();
    }

    /// <summary>
    /// 加入游戏
    /// </summary>
    /// <param name="data"></param>
    public void ServerCreateJoinRoom(TenCreateJoinRoomData data)
    {
        CleanAllDesk();
        UpdateBaseScore(TenModel.Inst.mRoomRules.baseScore);
        if (data.roomInfo.pt)
        {
            mRoomId.text = "模式：自由场";
            mSelfPlayer.SetChangDeskBtnState(true);
            mSelfPlayer.SetInvateBtnState(false);
        }
        else
        {
            mRoomId.text = "房间号：" + data.roomInfo.roomId;
            mSelfPlayer.SetChangDeskBtnState(false);
            mSelfPlayer.SetInvateBtnState(true);
        }

        if (data.roomInfo.playerList != null)
        {//玩家坐下
            for (int i = 0; i < data.roomInfo.playerList.Count; i++)
            {
                PlayerSeatDown(data.roomInfo.playerList[i]);
            }
        }

        ChangZhuang(data.roomInfo.zhuangSeatId);

        if (data.roomInfo.roomState == 0 || (eTenGameState)data.roomInfo.gameState == eTenGameState.Ready)
        {
            if (TenModel.Inst.mPlayerInfoDic[data.roomInfo.mySeatId].isReady)
            {
                mSelfPlayer.SetReadybtnState(false);
            }
            else {
                mSelfPlayer.SetReadybtnState(true);
            }
        }
        else {
            TenPlayerUI player = null;
            mSelfPlayer.SetReadybtnState(false);
            mSelfPlayer.SetInvateBtnState(false);
            mSelfPlayer.SetChangDeskBtnState(false);

            if (data.roomInfo.handCardsList != null && data.roomInfo.handCardsList.myCardsInfo != null) {
                if (TryGetPlayer(data.roomInfo.mySeatId, out player))
                {
                    if (data.roomInfo.handCardsList.myCardsInfo.cards != null)
                    {
                        player.InstantiateCards(data.roomInfo.mySeatId, data.roomInfo.handCardsList.myCardsInfo.cards.cards,false, data.roomInfo.handCardsList.myCardsInfo.cards.mp);
                    }

                    player.UpdateCathecticCoin(data.roomInfo.handCardsList.myCardsInfo.xz + "");

                    if (data.roomInfo.handCardsList.myCardsInfo.isQz) {
                        player.SetQiangZhuangResult(true, data.roomInfo.handCardsList.myCardsInfo.qzbs);
                    }

                    if (data.roomInfo.handCardsList.myCardsInfo.cardsType!=null) {
                        player.SetCardsType(data.roomInfo.handCardsList.myCardsInfo.cardsType);
                    }
                    

                    switch ((eTenGameState)data.roomInfo.gameState)
                    {
                        case eTenGameState.XiaZhu:
                            if (data.roomInfo.handCardsList.myCardsInfo.isXz)
                            {
                                player.UpdateCathecticCoin(data.roomInfo.handCardsList.myCardsInfo.xz + "");
                            }
                            else
                            {
                                if (data.roomInfo.zhuangSeatId != data.roomInfo.mySeatId) {
                                    mSelfPlayer.SetBetBtnItemState(true);
                                    mSelfPlayer.InitOptItemList<float>(data.roomInfo.handCardsList.myCardsInfo.xzListValue);
                                    mSelfPlayer.ShowBetBtnList(data.roomInfo.handCardsList.myCardsInfo.canXzList);
                                }
                            }
                            break;
                        case eTenGameState.QiangZhuang:
                            if (data.roomInfo.handCardsList.myCardsInfo.isQz)
                            {
                                player.SetQiangZhuangResult(true, data.roomInfo.handCardsList.myCardsInfo.qzbs);
                            }
                            else
                            {
                                mSelfPlayer.SetBetBtnItemState(true);
                                Debug.Log(data.roomInfo);
                                if (data.roomInfo.handCardsList.myCardsInfo.qzListValue != null)
                                {
                                    mSelfPlayer.InitOptItemList<int>(data.roomInfo.handCardsList.myCardsInfo.qzListValue);
                                }
                                if(data.roomInfo.handCardsList.myCardsInfo.canQzList != null)
                                {
                                    mSelfPlayer.ShowBetBtnList(data.roomInfo.handCardsList.myCardsInfo.canQzList);
                                }
                                
                            }
                            break;
                        case eTenGameState.LookCard:
                            if (data.roomInfo.handCardsList.myCardsInfo.isLp && data.roomInfo.handCardsList.myCardsInfo.cardsType != null)
                            {
                                player.SeparateCards(data.roomInfo.handCardsList.myCardsInfo.cardsType.order);
                                player.SetCardType(true, data.roomInfo.handCardsList.myCardsInfo.cardsType.point, data.roomInfo.handCardsList.myCardsInfo.cardsType.ratio);
                            }
                            else
                            {
                                TenModel.Inst.mLookCard = true;
                                //mSelfPlayer.SetLiangCardBtnState(true);
                               // mSelfPlayer.SetCuoBtnState(false);
                            }
                            break;
                    }
                }
            }


            if (data.roomInfo.handCardsList != null && data.roomInfo.handCardsList.otherCardsInfo != null) {
                for (int i=0;i < data.roomInfo.handCardsList.otherCardsInfo.Count;i++) {
                    if (TryGetPlayer(data.roomInfo.handCardsList.otherCardsInfo[i].seatId,out player)) {

                        if (data.roomInfo.handCardsList.otherCardsInfo[i].cards!=null) {
                            player.InstantiateCards(data.roomInfo.handCardsList.otherCardsInfo[i].seatId,
                                data.roomInfo.handCardsList.otherCardsInfo[i].cards.cards,false, data.roomInfo.handCardsList.otherCardsInfo[i].cards.mp);
                        }

                        player.UpdateCathecticCoin(data.roomInfo.handCardsList.otherCardsInfo[i].xz + "");

                        if (data.roomInfo.handCardsList.otherCardsInfo[i].isQz)
                        {
                            player.SetQiangZhuangResult(true, data.roomInfo.handCardsList.otherCardsInfo[i].qzbs);
                        }

                        switch ((eTenGameState)data.roomInfo.gameState) {
                            case eTenGameState.XiaZhu:
                                if (data.roomInfo.handCardsList.otherCardsInfo[i].isXz) {
                                    player.UpdateCathecticCoin(data.roomInfo.handCardsList.otherCardsInfo[i].xz+"");
                                }
                                break;
                            case eTenGameState.QiangZhuang:
                                if (data.roomInfo.handCardsList.otherCardsInfo[i].isQz) {
                                    player.SetQiangZhuangResult(true, data.roomInfo.handCardsList.otherCardsInfo[i].qzbs);
                                }
                                break;
                            case eTenGameState.LookCard:
                                if (data.roomInfo.handCardsList.otherCardsInfo[i].isLp && data.roomInfo.handCardsList.otherCardsInfo[i].cardsType!=null) {

                                    List<string> cards = new List<string>();
                                    cards.AddRange(data.roomInfo.handCardsList.otherCardsInfo[i].cards.mp);
                                    cards.AddRange(data.roomInfo.handCardsList.otherCardsInfo[i].cards.cards);

                                    player.TurnCards(cards);
                                    player.SeparateCards(data.roomInfo.handCardsList.otherCardsInfo[i].cardsType.order);
                                    player.SetCardType(true, data.roomInfo.handCardsList.myCardsInfo.cardsType.point, data.roomInfo.handCardsList.myCardsInfo.cardsType.ratio);

                                }
                                break;
                        }
                    }
                }
            }
        }

        if (data.roomInfo.zhuangSeatId>0) {
            CleanQzStateBut(data.roomInfo.zhuangSeatId);
        }

        switch ((eTenGameState)data.roomInfo.gameState) {
            case eTenGameState.LookCard:
                ShowLastTime("看牌倒计时", data.roomInfo.timeDown);
                break;
            case eTenGameState.QiangZhuang:
                ShowLastTime("抢庄倒计时", data.roomInfo.timeDown);
                break;
            case eTenGameState.XiaZhu:
                ShowLastTime("下注倒计时", data.roomInfo.timeDown);
                break;
        }
    }

    /// <summary>
    /// 玩家坐下
    /// </summary>
    /// <param name="data"></param>
    public void NetOnPlayerSeatDown(TenOnPlayerSeatDown data)
    {
        PlayerSeatDown(data.player);
    }

    /// <summary>
    /// 有玩家离开
    /// </summary>
    /// <param name="seatId"></param>
    public void NetOnPlayerLeave(int seatId)
    {
        TenPlayerUI player = null;

        GameObject widget = null;
        if (BaseView.childrenWidgetDic.TryGetValue(typeof(GameUserInfoWidget).Name, out widget))
        {
            if (widget!=null) {
                GameUserInfoWidget infoWidget = widget.GetComponent<GameUserInfoWidget>();
                if (infoWidget != null)
                {
                    if (infoWidget.GetSeatId() == seatId)
                    {
                        BaseViewWidget.CloseWidget<GameUserInfoWidget>();
                    }
                }
            }
        }

        if (TryGetPlayer(seatId, out player))
        {
            player.CleanHandCards();
            player.CleanPlayer();
        }

        mPlayerDic.Remove(seatId);
    }

    /// <summary>
    /// 有玩家准备
    /// </summary>
    /// <param name="seatId"></param>
    public void NetOnPlayerReady(int seatId)
    {
        TenPlayerUI player = null;
        if (TryGetPlayer(seatId, out player))
        {
            player.SetReadyState(true);
        }

        if (seatId == TenModel.Inst.mMySeatId)
        {
            mSelfPlayer.SetReadybtnState(false);
        }
    }

    /// <summary>
    /// 游戏开始倒计时
    /// </summary>
    /// <param name="time"></param>
    public void NetOnGameStartLastTime(int time)
    {
        if (time < 0)
        {
            mLastTime = 0;
            mLastTimeGo.gameObject.SetActive(false);
            Global.Inst.GetController<CommonTipsController>().ShowTips("游戏未能开始");
        }
        else
        {
            ShowLastTime("游戏即将开始", time);
        }
    }

    /// <summary>
    /// 玩家上下线
    /// </summary>
    /// <param name="data"></param>
    public void NetOnOnOffLine(TenOnPlayerOffLine data)
    {
        TenPlayerUI player = null;
        if (TryGetPlayer(data.seatId, out player))
        {
            player.SetOffLineState(data.state == 1 ? false : true);
        }
    }

    /// <summary>
    /// 游戏聊天
    /// </summary>
    /// <param name="chat"></param>
    public void NetOnGameTalk(SendReceiveGameChat chat)
    {
        TenPlayerUI player = null;
        if (TryGetPlayer(chat.fromSeatId, out player))
        {
            player.ServerGameChat(chat);
        }
    }

    /// <summary>
    /// 游戏开始
    /// </summary>
    /// <param name="seatId"></param>
    public void NetOnGameStart(int seatId)
    {
        CleanDesk();
        List<int> list = new List<int>();
        list.AddRange(mPlayerDic.Keys);

        for (int i = 0; i < list.Count; i++)
        {
            mPlayerDic[list[i]].SetZhuangState(false);
            mPlayerDic[list[i]].SetReadyState(false);
        }

        TenPlayerUI player = null;

        if (TryGetPlayer(seatId, out player))
        {
            player.SetZhuangState(true);
        }

        mSelfPlayer.SetReadybtnState(false);
        mSelfPlayer.SetInvateBtnState(false);
        mSelfPlayer.SetChangDeskBtnState(false);
        //
        mSelfPlayer.HidePlayerPoint();
    }

    /// <summary>
    /// 自己得到操作指令
    /// </summary>
    /// <param name="data"></param>
    public void NetOnSelfOpt(TenOnSelfOpt data)
    {
        StartCoroutine(IEOnSelfOpt(data));
    }

    /// <summary>
    /// 玩家操作结果
    /// </summary>
    /// <param name="data"></param>
    public void NetOnPlayerOptResult(TenOnPlayerOptResult data)
    {
        switch ((eTenOpt)data.ins)
        {
            case eTenOpt.QZ://抢庄
                OnPlayerQiangZhuang(data);
                break;
            case eTenOpt.XZ://下注
                OnPlayerXiaZhu(data);
                break;
            case eTenOpt.LP://亮牌
                StartCoroutine(OnPlayerLiangCard(data));
                break;
            case eTenOpt.YP:
                Debug.Log("要牌啦");
                StartCoroutine(IEOtherCastCard(data.seatId));
                break;

            case eTenOpt.TP:
                Debug.Log("停牌啦");
                mSelfPlayer.SetLiangCardBtnState(false);
                mSelfPlayer.SetCuoBtnState(false);
                break;
        }
    }

    /// <summary>
    /// 同步玩家发牌
    /// </summary>
    /// <param name="data"></param>
    public void NetOnCastCard(TenonCastCard data)
    {
        StartCoroutine(IEOnCastCard(data));
    }

    /// <summary>
    /// 小结算
    /// </summary>
    /// <param name="data"></param>
    public void NetOnSmallSettle(TenonSmallSettle data)
    {
        mSelfPlayer.SetLiangCardBtnState(false);
        if (mNiuniuCoinFlyWidget == null)
        {
            mNiuniuCoinFlyWidget = BaseView.GetWidget<TenCoinFlyWidget>(AssetsPathDic.TenCoinFlyWidget, transform);
        }
        ShowLastTime("准备倒计时", data.lastTime);
        StartCoroutine(OnSmallSettle(data));
    }

    /// <summary>
    /// 换庄
    /// </summary>
    /// <param name="data"></param>
    public void NetOnChangZhuang(TenOnChangZhuang data)
    {
        if (data.random) {
            StartCoroutine(StartRandomZhuang(data.zhuangSeatId, data.randomSeatList));
        }
        else {
            ChangZhuang(data.zhuangSeatId);
            //开始声音
            SoundProcess.PlaySound("Ten/SDB_wrnn_start");
        }
    }

    /// <summary>
    /// 自动换桌
    /// </summary>
    public void NetOnAutoChangDesk() {
        mSelfPlayer.SetChangDeskBtnState(false);
        mSelfPlayer.SetReadyBtnState(false);
        mSelfPlayer.SetChangDeskBtnState(false);
    }

    #endregion


    #region 游戏逻辑的协程处理

    /// <summary>
    /// 协程延迟发牌
    /// </summary>
    private IEnumerator IEOnCastCard(TenonCastCard data) {
        mCastDown = false;
        int cardNum = 0;
        yield return new WaitUntil(()=> {
            return mRandomZhuangDown == true;
        });
        if (data.selfCards != null && TenModel.Inst.mGameedSeatIdList != null)
        {

            List<string> Othercards = new List<string>();
            for (int i = 0; i < data.selfCards.Count; i++)
            {
                Othercards.Add("0");
            }

            for (int i = 0; i < TenModel.Inst.mGameedSeatIdList.Count; i++)
            {
                TenPlayerUI player = null;

                if (TryGetPlayer(TenModel.Inst.mGameedSeatIdList[i], out player))
                {

                    if (TenModel.Inst.mGameedSeatIdList[i] == TenModel.Inst.mMySeatId)
                    {
                        if (data.cardsType != null)
                        {
                            player.SetCardsType(data.cardsType);
                        }
                        /*if (data.selfCards.Count == 4)
                        {
                            player.CastCardWithAnim(TenModel.Inst.mGameedSeatIdList[i], data.selfCards, true);
                        }
                        else*/
                        {
                            player.CastCardWithAnim(TenModel.Inst.mGameedSeatIdList[i], data.selfCards, true);
                        }

                        cardNum = player.GetHandCardsNum();
                    }
                    else
                    {
                        player.CastCardWithAnim(TenModel.Inst.mGameedSeatIdList[i], Othercards, false);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.2f * data.selfCards.Count);
        mCastDown = true;

        if (data.canShowCard)
        {
            if (TenModel.Inst.mGameed)
            {
                mSelfPlayer.SetLiangCardBtnState(true);
                mSelfPlayer.SetCuoBtnState(true);
            }
            ShowLastTime("要牌倒计时", data.lastTime - 2);
        }

        //显示点数
        mSelfPlayer.ShowPlayerPoint(data.cardsType.point, cardNum);

    }



    private IEnumerator IEOtherCastCard(int seatId)
    {
        mCastDown = false;
        yield return new WaitUntil(() => {
            return mRandomZhuangDown == true;
        });
 

        List<string> Othercards = new List<string>();

        Othercards.Add("0");
        //
        TenPlayerUI player = null;
        if (TryGetPlayer(seatId, out player))
        {
           player.CastCardWithAnim(seatId, Othercards, false);                
        }
        yield return new WaitForSeconds(0.2f * 1);
        mCastDown = true;

        /*if (data.canShowCard)
        {
            if (TenModel.Inst.mGameed)
            {
                mSelfPlayer.SetLiangCardBtnState(true);
                mSelfPlayer.SetCuoBtnState(true);
            }
            ShowLastTime("亮牌倒计时", data.lastTime - 2);
        }*/
    }




    /// <summary>
    /// 自己得到操作
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private IEnumerator IEOnSelfOpt(TenOnSelfOpt data) {
        yield return new WaitUntil(() =>
        {
            return mRandomZhuangDown == true;
        });
        yield return new WaitUntil(() =>
        {
            return mCastDown == true;
        });
        switch ((eTenOpt)data.ins)
        {
            case eTenOpt.Nil://同步房间状态
                switch ((eTenGameState)data.gameState)
                {
                    case eTenGameState.LookCard:
                        ShowLastTime("要牌倒计时", data.lastTime - 2);
                        break;
                    case eTenGameState.QiangZhuang:
                        ShowLastTime("抢庄倒计时", data.lastTime - 2);
                        break;
                    case eTenGameState.Ready:
                        ShowLastTime("准备倒计时", data.lastTime - 2);
                        break;
                    case eTenGameState.XiaZhu:
                        ShowLastTime("下注倒计时", data.lastTime - 2);
                        break;
                }
                break;
            case eTenOpt.QZ://抢庄
                ShowLastTime("抢庄倒计时", data.lastTime - 2);
                mSelfPlayer.SetBetBtnItemState(true);
                mSelfPlayer.InitOptItemList<int>(data.qzListValue);
                mSelfPlayer.ShowBetBtnList(data.canQzList);
                break;
            case eTenOpt.XZ://下注
                ShowLastTime("下注倒计时", data.lastTime - 2);
                mSelfPlayer.SetBetBtnItemState(true);
                mSelfPlayer.InitOptItemList<float>(data.xzListValue);
                mSelfPlayer.ShowBetBtnList(data.canXzList);
                CleanQzStateBut(TenModel.Inst.mZhuangSeatId);
                break;
            case eTenOpt.LP://亮牌
                ShowLastTime("要牌倒计时", data.lastTime - 2);
                mSelfPlayer.SetLiangCardBtnState(true);
                mSelfPlayer.SetCuoBtnState(true);
                break;
            case eTenOpt.YP: //要牌
                ShowLastTime("要牌倒计时", data.lastTime - 2);
                mSelfPlayer.SetLiangCardBtnState(true);
                mSelfPlayer.SetCuoBtnState(true);
                break;
        }
    }


    #endregion


    #region 同步玩家操作结果

    /// <summary>
    /// 同步玩家亮牌
    /// </summary>
    /// <param name="data"></param>
    private IEnumerator OnPlayerLiangCard(TenOnPlayerOptResult data)
    {

        bool isSelf = (data.seatId == TenModel.Inst.mMySeatId);

        if (isSelf)
        {
            TenModel.Inst.mLookCard = true;
            mSelfPlayer.HideLiangCuoState();
            if (BaseView.childrenWidgetDic.ContainsKey(typeof(TenCuoCardWidget).Name)) {
                BaseViewWidget.CloseWidget<TenCuoCardWidget>();
            }
        }


        TenPlayerUI player = null;
        if (TryGetPlayer(data.seatId, out player))
        {
            if (player.GetTurnState())
            {
                yield break;
            }
            player.TurnCards(data.cards);
            yield return new WaitForSeconds(0.7f);
            player.SeparateCards(data.cardsType.order);

            player.SetCardType(true, data.cardsType.point, data.cardsType.ratio, isSelf);
        }
    }

    /// <summary>
    /// 同步玩家抢庄
    /// </summary>
    /// <param name="data"></param>
    private void OnPlayerQiangZhuang(TenOnPlayerOptResult data)
    {
        if (data.seatId == TenModel.Inst.mMySeatId)
        {
            mSelfPlayer.SetBetBtnItemState(false);
        }

        TenPlayerUI player = null;
        if (TryGetPlayer(data.seatId, out player))
        {
            player.SetQiangZhuangResult(true, data.qzValue);
        }

    }


    /// <summary>
    /// 同步玩家下注
    /// </summary>
    /// <param name="data"></param>
    private void OnPlayerXiaZhu(TenOnPlayerOptResult data)
    {
        if (data.seatId == TenModel.Inst.mMySeatId)
        {
            mSelfPlayer.SetBetBtnItemState(false);
        }
        TenPlayerUI player = null;
        if (mNiuniuCoinFlyWidget == null)
        {
            mNiuniuCoinFlyWidget = BaseView.GetWidget<TenCoinFlyWidget>(AssetsPathDic.TenCoinFlyWidget, transform);
        }
        if (TryGetPlayer(data.seatId, out player))
        {
            player.UpdateCathecticCoin(data.xzValue + "");
            mNiuniuCoinFlyWidget.SetCoinFly(player.GetBaseInfoPos(), player.GetChePosition(), 1, 0.3f);
        }
    }

    /// <summary>
    /// 同步小结算
    /// </summary>
    /// <param name="data"></param>
    private IEnumerator OnSmallSettle(TenonSmallSettle data)
    {
        List<int> winList = new List<int>();
        List<int> loseList = new List<int>();

        yield return new WaitForSeconds(1.5f);

        FlySmallSettleCoin(data.winList);

        yield return new WaitForSeconds(1.0f);

        FlySmallSettleCoin(data.lostList);

        TenPlayerUI player = null;

        if (data.scoreList!=null) {
            for (int i = 0; i < data.scoreList.Count; i++)
            {
                if (TryGetPlayer(data.scoreList[i].seatId, out player))
                {
                    player.SetWinLoseScore(data.scoreList[i].score);
                }
            }
        }

        if (data.lastScore!=null) {
            for (int i=0;i<data.lastScore.Count;i++) {
                UpdatePlayerScore(data.lastScore[i].seatId,data.lastScore[i].score);
                if (data.lastScore[i].seatId == TenModel.Inst.mMySeatId) {
                    UpdateTotalWinScore(data.lastScore[i].totalWin);
                }
            }
        }

        DelayRun(5.0f, ()=> {
            mSelfPlayer.SetReadybtnState(true);
            if (TenModel.Inst.mGoldPattern)
            {
                mSelfPlayer.SetChangDeskBtnState(true);
            }
            else
            {
                mSelfPlayer.SetChangDeskBtnState(false);
            }
            CleanDesk();
        } );
    }


    #endregion


    #region 辅助函数

    /// <summary>
    /// 通过座位号得到玩家ui
    /// </summary>
    /// <param name="seatId"></param>
    /// <returns></returns>
    public bool TryGetPlayer(int seatId, out TenPlayerUI player)
    {
        return mPlayerDic.TryGetValue(seatId, out player);
    }

    /// <summary>
    /// 玩家坐下
    /// </summary>
    /// <param name="player"></param>
    private void PlayerSeatDown(TenPlayerInfo player)
    {
        int index = 0;
        if (player.seatId == TenModel.Inst.mMySeatId)
        {
            if (player.isReady)
            {
                mSelfPlayer.SetReadybtnState(false);
            }
            else
            {
                mSelfPlayer.SetReadybtnState(true);
            }
            index = 0;
            UpdateTotalWinScore(player.totalWin);
        }
        else if (player.seatId - TenModel.Inst.mMySeatId > 0)
        {
            index = player.seatId - TenModel.Inst.mMySeatId;
        }
        else
        {
            index = player.seatId - TenModel.Inst.mMySeatId + mAllPlayers.Length;
        }

        //Debug.Log("player.headUrl, player.nickname=" + player.headUrl + player.nickname);
        mAllPlayers[index].InitPlayer(player.headUrl, player.nickname, player.userId, player.score, player.isReady, player.onLineState == 1 ? false : true);
        mAllPlayers[index].SeatId = player.seatId;
        if (mPlayerDic.ContainsKey(player.seatId))
        {
            mPlayerDic[player.seatId] = mAllPlayers[index];
        }
        else
        {
            mPlayerDic.Add(player.seatId, mAllPlayers[index]);
        }
    }


    /// <summary>
    /// 显示倒计时
    /// </summary>
    private void ShowLastTime(string content, int lastTime)
    {

        mLastTime = lastTime;
        mLastTimeContent = content;
        mLastTimeGo.SetActive(true);
        mLastTimeGo.GetComponentInChildren<UILabel>().text = content + "(" + mLastTime + ")";
        if (IsInvoking("InvokeLastTime"))
        {
            CancelInvoke("InvokeLastTime");
        }
        if (lastTime > 0)
        {
            InvokeRepeating("InvokeLastTime", 0.01f, 1.0f);
        }
        else
        {
            mLastTimeGo.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示倒计时
    /// </summary>
    private void InvokeLastTime()
    {
        mLastTimeGo.GetComponentInChildren<UILabel>().text = mLastTimeContent + "(" + mLastTime + ")";
        mLastTime -= 1;
        if (mLastTime < 0)
        {
            mLastTimeGo.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 清理抢庄状态，除了庄家
    /// </summary>
    /// <param name="SeatId"></param>
    private void CleanQzStateBut(int SeatId = 0)
    {
        TenPlayerUI player = null;
        for (int i = 0; i < TenModel.Inst.mGameedSeatIdList.Count; i++)
        {
            if (TenModel.Inst.mGameedSeatIdList[i] != SeatId)
            {
                if (TryGetPlayer(TenModel.Inst.mGameedSeatIdList[i], out player))
                {
                    player.SetQiangZhuangResult(false, 0);
                }
            }
        }
    }

    /// <summary>
    /// 添加玩家输赢分数
    /// </summary>
    /// <param name="list"></param>
    /// <param name="win"></param>
    /// <param name="lose"></param>
    private void AddPlayerScore(List<TenonSmallSettleWinLoseList> list, ref Dictionary<int, float> win, ref Dictionary<int, float> lose)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (win.ContainsKey(list[i].winSeatId))
            {
                win[list[i].winSeatId] += list[i].winScore;
            }
            else
            {
                win.Add(list[i].winSeatId, list[i].winScore);
            }

            if (lose.ContainsKey(list[i].loseSeatId))
            {
                lose[list[i].loseSeatId] -= list[i].loseScore;
            }
            else
            {
                lose.Add(list[i].loseSeatId, -list[i].loseScore);
            }
        }
    }

    /// <summary>
    /// 小结算飞金币
    /// </summary>
    /// <param name="list"></param>
    /// <param name="winDic"></param>
    /// <param name="loseDic"></param>
    /// <param name="winList"></param>
    /// <param name="loseList"></param>
    private void FlySmallSettleCoin(List<TenonSmallSettleWinLoseList> list) {

        TenPlayerUI winPlayer = null;
        TenPlayerUI losePlayer = null;

        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (TryGetPlayer(list[i].winSeatId, out winPlayer) && TryGetPlayer(list[i].loseSeatId, out losePlayer))
                {
                    mNiuniuCoinFlyWidget.SetCoinFly(losePlayer.GetBaseInfoPos(), winPlayer.GetBaseInfoPos(), 5, 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// 更新玩家金币
    /// </summary>
    /// <param name="seatId"></param>
    /// <param name="score"></param>
    private void UpdatePlayerScore(int seatId,float score) {
        TenPlayerUI player = null;
        if (TryGetPlayer(seatId,out player)) {
            player.SetUserScore(score);
        }
    }

    /// <summary>
    /// 底分
    /// </summary>
    /// <param name="value"></param>
    private void UpdateBaseScore(float value) {
        mBaseScoreLabel.text = "底分："+value.ToString();
    }

    /// <summary>
    /// 更新总的输赢
    /// </summary>
    /// <param name="value"></param>
    private void UpdateTotalWinScore(float value) {
        mTotalWinLabel.text =""+ value.ToString();
    }

    /// <summary>
    /// 换庄
    /// </summary>
    /// <param name="seatId"></param>
    private void ChangZhuang(int seatId) {
        for (int i = 0; i < TenModel.Inst.mSeatList.Count; i++)
        {
            TenPlayerUI player = null;
            if (TryGetPlayer(TenModel.Inst.mSeatList[i], out player))
            {
                player.SetZhuangState(false);
                if (TenModel.Inst.mSeatList[i] == seatId)
                {
                    player.SetZhuangState(true);
                }
                else {
                    player.SetQiangZhuangResult(false, 0);

                }
            }
        }


        
    }

    /// <summary>
    /// 得到自己手牌
    /// </summary>
    /// <returns></returns>
    public List<string> GetSelfCards() {
        TenPlayerUI player = null;
        if (TryGetPlayer(TenModel.Inst.mMySeatId,out player)) {
            return player.GetHandCard();
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 自己翻牌
    /// </summary>
    public void TurnSelfCards() {
        TenPlayerUI player = null;
        if (TryGetPlayer(TenModel.Inst.mMySeatId, out player))
        {
            player.TurnCards(player.GetHandCard());
            TenPlayerHandCardsType type = player.GetCardsType();
            if (type!=null) {
                player.SetCardType(true, type.point, type.ratio, true);
                player.SeparateCards(type.order);
            }
        }
    }

    /// <summary>
    /// 随机选庄
    /// </summary>
    /// <param name="seatId"></param>
    private IEnumerator StartRandomZhuang(int seatId,List<int> list) {
        mRandomZhuangDown = false;
        TenPlayerUI player = null;

        for (int k=0;k<4;k++) {
            for (int i = 0; i < list.Count; i++)
            {
                if (TryGetPlayer(list[i], out player))
                {
                    player.SetRandomZhuangAnimState(true);
                    yield return new WaitForSeconds(0.15f);
                    player.SetRandomZhuangAnimState(false);
                }
            }
        }

        for (int i = 0; i < TenModel.Inst.mSeatList.Count; i++)
        {
            if (TryGetPlayer(TenModel.Inst.mSeatList[i], out player))
            {
                player.SetZhuangState(false);
                if (TenModel.Inst.mSeatList[i] != seatId)
                {
                    player.SetQiangZhuangResult(false, 0);
                }
            }
        }

        for (int i = 0; i < TenModel.Inst.mGameedSeatIdList.Count; i++)
        {

            if (TryGetPlayer(TenModel.Inst.mGameedSeatIdList[i], out player))
            {
                player.SetRandomZhuangAnimState(false);
            }
        }

        if (TryGetPlayer(seatId, out player)) {
            mFlyZhuang.gameObject.SetActive(true);
            mFlyZhuang.gameObject.transform.position = mFlyZhuangPosition;
            Hashtable args = new Hashtable();
            List<object> finishargs = new List<object>();
            args.Add("easeType", iTween.EaseType.linear);
            args.Add("time", 0.4f);
            args.Add("oncomplete", "OnRandomZhuangFlyFinish");
            args.Add("oncompleteparams", seatId);
            args.Add("oncompletetarget", gameObject);
            args.Add("position", player.GetZhuangPosition());
            iTween.MoveTo(mFlyZhuang, args);
        }
        yield return new WaitForSeconds(0.4f);
        mRandomZhuangDown = true;

        //开始声音
        SoundProcess.PlaySound("Ten/SDB_wrnn_start");

    }

    /// <summary>
    /// 随机庄移动结束
    /// </summary>
    /// <param name="seatId"></param>
    private void OnRandomZhuangFlyFinish(int seatId) {
        mFlyZhuang.gameObject.SetActive(false);
        TenPlayerUI player = null;
        if (TryGetPlayer(seatId, out player))
        {
            player.SetZhuangState(true);
        }
    }

    #endregion

    void OnApplicationPause(bool b)
    {
        if (b)//最小化游戏
        {
            SQDebug.Log("程序失去焦点");
            NetProcess.ReleaseAllConnect();
            GameManager.Instance.ResetConnetTime();
        }
    }

}