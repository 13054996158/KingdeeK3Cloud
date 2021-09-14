using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.Core;
//服务端
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
//校验器
using Kingdee.BOS.Core.Validation;
using System.ComponentModel;
using System.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.FormElement;
//using System.ComponentModel.Composition;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Metadata;



namespace BHR.K3.FIN.CN.Business.PlugIn
{
   public class ReverseTheVoucherNumber: AbstractOperationServicePlugIn     //AfterExecuteOperationTransaction
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            int i;
            //读取全部的单据,for循环,转换成DynamicObject类型
            foreach (DynamicObject entity in e.DataEntitys)
            {   //如果不为空,开始循环
                if (entity != null)
                {
                    //单据转换,上游单据是什么,下游单据是什么
                    var rules = ConvertServiceHelper.GetConvertRules(this.Context, "PUR_PurchaseOrder", "PUR_ReceiveBill");
                    //默认的单据转换
                    var rule = rules.FirstOrDefault(t => t.IsDefault);
                    //获取每个单据体,获取分录ID,判断是哪一行
                    DynamicObjectCollection entry = (DynamicObjectCollection)entity["POOrderEntry"];
                    i = 0;
                    ConvertOperationResult operationResult = null;
                    ListSelectedRow[] selectedRows = new ListSelectedRow[entry.Count];
                    //把选中的单据体ID,循环,全部读取出来
                    foreach (DynamicObject HHentity in entry)
                    {
                        selectedRows[i] = new ListSelectedRow(Convert.ToString(entity["id"]), Convert.ToString(HHentity["id"]), i, "PUR_PurchaseOrder");
                        i++;
                    }
                    Dictionary<string, object> custParams = new Dictionary<string, object>();
                    //下推事件
                    PushArgs pushArgs = new PushArgs(rule, selectedRows)
                    {
                        //标准发货通知单
                        // 请设定目标单据单据类型。如无单据类型，可以空字符
                        TargetBillTypeId = "",
                        // 请设定目标单据主业务组织。如无主业务组织，可以为0
                        TargetOrgId = 0,
                        // 可以传递额外附加的参数给单据转换插件，如无此需求，可以忽略
                        CustomParams = custParams,
                    };
                    operationResult = ConvertServiceHelper.Push(this.Context, pushArgs, OperateOption.Create());
                    // 获取生成的目标单据数据包
                    DynamicObject[] objs = (from p in operationResult.TargetDataEntities
                                            select p.DataEntity).ToArray();
                    // 读取目标单据元数据
                    var targetBillMeta = MetaDataServiceHelper.Load(this.Context, "PUR_ReceiveBill") as FormMetadata;
                    OperateOption saveOption = OperateOption.Create();
                    // 忽略全部需要交互性质的提示，直接保存；调用保存事件
                    var saveResult = BusinessDataServiceHelper.Save(this.Context, targetBillMeta.BusinessInfo, objs, saveOption, "Save");
                }
            }
        }
        

    }
}
