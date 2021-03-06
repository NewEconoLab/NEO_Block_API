﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NEO_Block_API.lib;

namespace NEO_Block_API.Controllers
{
    public class Claim
    {
        mongoHelper mh = new mongoHelper();

        public JArray getClaimGasUtxoList(string mongodbConnStr, string mongodbDatabase, string address, bool isGetUsed = true, int pageNum=1, int pageSize=10)
        {
            string findFliter = string.Empty;
            if (isGetUsed)
            {
                //已使用，未领取(可领取GAS)
                findFliter = "{addr:'" + address + "','asset':'0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b','used':{$ne:''},'claimed':''}";
            }
            else
            {
                //未使用，未领取(不可领取GAS)
                findFliter = "{addr:'" + address + "','asset':'0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b','used':'','claimed':''}";
            }

            //统计有多少NEO UTXO
            long utxoCount = mh.GetDataCount(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
            if (utxoCount == 0) return new JArray { new JObject() { { "count", 0 }, { "list", new JArray { } } } };
            //
            string sortStr = new JObject() { { "createHeight", 1 } }.ToString();
            var gasIssueJA = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "utxo", sortStr, pageSize, pageNum, findFliter);

            int curHeight = (int)mh.Getdatablockheight(mongodbConnStr, mongodbDatabase).First()["blockDataHeight"];
            var res = gasIssueJA.Select(utxo => {
                int start = (int)utxo["createHeight"];
                int end = (isGetUsed ? (int)utxo["useHeight"] - 1 : curHeight);
                int value = (int)utxo["value"];
                decimal issueSysfee = mh.GetTotalSysFeeByBlock(mongodbConnStr, mongodbDatabase, end) - mh.GetTotalSysFeeByBlock(mongodbConnStr, mongodbDatabase, start - 1);//转入的这个块，属于当前地址，由于差额计算方法，需要开始-1
                decimal issueGasInBlock = countGas(start, end);
                decimal issueGas = (issueSysfee + issueGasInBlock) / 100000000 * value;
                //
                var jo = (JObject)utxo;
                jo.Add("gas", issueGas.ToString());
                return jo;
            }).ToArray();

            return new JArray{new JObject()
            {
                { "count", utxoCount},
                { "list", new JArray{res } }
            } };

        }

        public JObject getClaimGas(string mongodbConnStr, string mongodbDatabase,string address,bool isGetUsed = true, bool isLimit50Flag=true, bool isIgnoreJA=false)
        {
            decimal issueGas = 0;

            string findFliter = string.Empty;
            if (isGetUsed)
            {
                //已使用，未领取(可领取GAS)
                findFliter = "{addr:'" + address + "','asset':'0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b','used':{$ne:''},'claimed':''}";
            }
            else
            {
                //未使用，未领取(不可领取GAS)
                findFliter = "{addr:'" + address + "','asset':'0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b','used':'','claimed':''}";
            }

            //统计有多少NEO UTXO
            long utxoCount = mh.GetDataCount(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
            if (utxoCount == 0) return new JObject() { { "errorCode", "No data" } };

            JObject J = new JObject();

            //只有UTXO小于等于50才处理
            int UTXOThreshold = 50;
            if ((isLimit50Flag && utxoCount <= UTXOThreshold) || !isLimit50Flag)
            {
                JArray gasIssueJA = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);


                foreach (JObject utxo in gasIssueJA)
                {
                    int start = (int)utxo["createHeight"];
                    int end = -1;
                    if (isGetUsed)
                    {
                        end = (int)utxo["useHeight"] - 1; //转出的这块的gas属于转入地址
                    }
                    else
                    {
                        //未花费以目前高度计算
                        end = (int)mh.Getdatablockheight(mongodbConnStr, mongodbDatabase).First()["blockDataHeight"];
                    }
                    int value = (int)utxo["value"];

                    decimal issueSysfee = mh.GetTotalSysFeeByBlock(mongodbConnStr, mongodbDatabase, end) - mh.GetTotalSysFeeByBlock(mongodbConnStr, mongodbDatabase, start - 1);//转入的这个块，属于当前地址，由于差额计算方法，需要开始-1
                    decimal issueGasInBlock = countGas(start, end);

                    issueGas += (issueSysfee + issueGasInBlock) / 100000000 * value;
                }

                J.Add("gas", issueGas);
                if(!isIgnoreJA)
                {
                    J.Add("claims", gasIssueJA);
                }
            }
            else
            {
                J.Add("errorCode", "-10");
                J.Add("errorMsg", "The data is too large to process");
                J.Add("errorData", "ClaimGas UTXO Threshold is " + UTXOThreshold);
            }

            return J;
        }

        //按1亿NEO计算
        private decimal countGas(int start, int end) {
            int step = 200 * 10000;
            int gasInBlock = 8;
            decimal gasCount = 0;
            for (int i = 0; i < 22; i++)
            {
                int forStart = i*step;
                int forEnd = (i+1)*step -1;

                //如果utxo使用高度小于循环开始高度，表示当前utxo都不在后续区间中
                if (end < forStart) { break; }

                //在区间内才计算
                if (start <= forEnd) {
                    if (start > forStart) { forStart = start; }
                    if (end < forEnd) { forEnd = end; }

                    gasCount += (forEnd - forStart + 1) * gasInBlock;
                }

                //每200万块，衰减一个gas，如果衰减到1则不变
                if (gasInBlock > 1) { gasInBlock--; }
            }

            return gasCount;
        }
    }
}
