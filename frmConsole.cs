using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Text;
using weCare.Core.Entity;
using weCare.Core.Utils;
using weCare.Core.Dac;
using System.Xml;

namespace weCare
{
    /// <summary>
    /// 孕产妇分娩信息上传.控制台
    /// </summary>
    public partial class frmConsole : Form
    {
        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        public frmConsole()
        {
            InitializeComponent();
        }
        #endregion

        #region 变量.属性

        /// <summary>
        /// 时间点
        /// </summary>
        string timePoint = " 03:00:00";
        string timePoint2 = " 05:00:00";
        #endregion

        #region 方法

        #region Init
        /// <summary>
        /// Init
        /// </summary>
        void Init()
        {
            this.progressBarControl.Visible = false;
            ///this.RefreshTask();
            this.gcTask.Dock = System.Windows.Forms.DockStyle.Fill;
        }
        #endregion

        #region RefreshTask
        /// <summary>
        /// RefreshTask
        /// </summary>
        void RefreshTask(string timePointstr)
        {
            int sortNo = 0;
            List<EntitySysTaskLog> data = this.GetTaskLog();
            data.Sort();
            foreach (EntitySysTaskLog item in data)
            {
                item.sortNo = ++sortNo;
            }
            this.gcTask.DataSource = data;

            string maxDate = string.Empty;
            if (data != null && data.Count > 0)
            {
                maxDate = data[data.Count - 1].execTime;
            }
            else
            {
                maxDate = DateTime.Now.ToString("yyyy-MM-dd") + timePointstr;
            }
            if (DateTime.Now > Convert.ToDateTime(maxDate))
            {
                this.gvTask.ViewCaption = "上传时间：" + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint;
            }
            else
            {
                this.gvTask.ViewCaption = "下次上传时间：" + maxDate;
            }
        }
        #endregion

        #region Upload
        /// <summary>
        /// Upload
        /// </summary>
        void Upload()
        {
            try
            {
                string Sql = string.Empty;
                IDataParameter[] parm = null;
                SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);
                string openDate = string.Empty;

                string todayStr = DateTime.Now.ToString("yyyy-MM-dd");

                Sql = @"select a.inpatientid, a.inpatientdate, a.opendate,b.patientid_chr
                          from inpatmedrec a
                           left join t_bse_patient b
                          on a.inpatientid = b.inpatientid_chr
                         where a.typeid = 'frmIMR_childbirth'
                           and a.status = '0'
                           and (a.opendate between to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                               to_date(?, 'yyyy-mm-dd hh24:mi:ss'))";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtRec = svc.GetDataTable(Sql, parm);

                Sql = @"select a.registerid, a.inpatientid, a.inpatientdate, a.uploaddate
                          from t_opr_bih_wacrecord a
                         where (a.uploaddate between to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                               to_date(?, 'yyyy-mm-dd hh24:mi:ss'))";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtUp = svc.GetDataTable(Sql, parm);

                Sql = @"select * from inpatmedrec_item a
                                where a.typeid = 'frmIMR_childbirth'
                                   and a.inpatientid = ?
                                   and a.opendate = to_date(?, 'yyyy-mm-dd hh24:mi:ss') ";

                List<EntityMother> lstMother = new List<EntityMother>();
                if (dtRec != null && dtRec.Rows.Count > 0)
                {
                    EntityMother vo = null;
                    DataRow[] drr = null;
                    foreach (DataRow drRec in dtRec.Rows)
                    {
                        vo = new EntityMother();
                        vo.lstChild = new List<EntityChild>();
                        EntityChild chilVo = new EntityChild();

                        vo.ipNo = drRec["inpatientid"].ToString();
                        openDate = drRec["opendate"].ToString();
                        vo.HISID = drRec["patientid_chr"].ToString();

                        if (dtUp != null && dtUp.Rows.Count > 0)
                        {
                            drr = dtUp.Select("inpatientid = '" + vo.ipNo + "'");
                            vo.flagId = ((drr != null && drr.Length > 0) ? 1 : 0);
                        }
                        else
                        {
                            vo.flagId = 0;
                        }

                        parm = svc.CreateParm(2);
                        parm[0].Value = vo.ipNo;
                        parm[1].Value = openDate;
                        DataTable dtMother = svc.GetDataTable(Sql, parm);

                        if (dtMother != null && dtMother.Rows.Count > 0)
                        {
                            foreach (DataRow drMother in dtMother.Rows)
                            {
                                #region Mother
                                // 床号
                                vo.BEDNO = "006";
                                // 分娩孕周(日) 
                                if (drMother["itemid"].ToString() == "m_cboPregnantWeek")
                                    vo.CYESISWEEK = drMother["itemcontent"].ToString();

                                #region 胎数及胎数代码
                                if (drMother["itemid"].ToString() == "m_cboLayNumber")
                                    vo.FETUSNUMBER_1 = drMother["itemcontent"].ToString().Substring(1, 1);    ////胎数
                                if (vo.FETUSNUMBER_1 == "1")
                                    vo.FETUSNUMBER_2 = "单胎";
                                if (vo.FETUSNUMBER_1 == "2")
                                    vo.FETUSNUMBER_2 = "双胎";
                                if (vo.FETUSNUMBER_1 == "3")
                                    vo.FETUSNUMBER_2 = "三胎";
                                if (Function.Dec(vo.FETUSNUMBER_1) >= 4)
                                    vo.FETUSNUMBER_2 = "四胎以上";
                                #endregion

                                #region   胎膜破裂方式名称 胎膜破裂方式代码
                                if (drMother["itemid"].ToString() == "radioButton1")
                                {
                                    vo.TAIMOPOLIEFANGSHI_1 = "人工"; //  胎膜破裂方式名称
                                    vo.TAIMOPOLIEFANGSHI_2 = "1"; //胎膜破裂方式代码;
                                }
                                if (drMother["itemid"].ToString() == "radioButton2")
                                {
                                    vo.TAIMOPOLIEFANGSHI_1 = "自然"; //  胎膜破裂方式名称
                                    vo.TAIMOPOLIEFANGSHI_2 = "0"; //胎膜破裂方式代码;
                                }
                                #endregion
                                // 胎膜破裂时间
                                if (drMother["itemid"].ToString() == "dateTimePicker2")
                                    vo.TAIMOPOLIE = Function.Datetime(drMother["itemcontent"]).ToString("yyyMMddHHmmss");

                                #region 分娩方式代码; 分娩方式
                                if (drMother["itemid"].ToString() == "m_rdbSuitableBirth")
                                {
                                    vo.CHIBIRTYPE_1 = "1";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "阴道自然分娩";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbDraught")
                                {
                                    vo.CHIBIRTYPE_1 = "2";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "阴道手术助产";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbClampBirth")
                                {
                                    vo.CHIBIRTYPE_1 = "21";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "产钳助产";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbHelpBirth")
                                {
                                    vo.CHIBIRTYPE_1 = "22";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "臀位助产";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbAttrahentBirth")
                                {
                                    vo.CHIBIRTYPE_1 = "23";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "胎头吸引";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbDissectBirth")
                                {
                                    vo.CHIBIRTYPE_1 = "3";          // 分娩方式代码; 分娩方式
                                    vo.CHIBIRTYPE_2 = "剖宫产";
                                }
                                #endregion

                                //必填

                                #endregion

                                #region Child

                                #region //婴儿性别代码  婴儿性别
                                if (drMother["itemid"].ToString() == "m_rdbMale")
                                {
                                    chilVo.SEX_1 = "1";
                                    chilVo.SEX_2 = "男";
                                }

                                if (drMother["itemid"].ToString() == "m_rdbFeMale")
                                {
                                    chilVo.SEX_1 = "2";
                                    chilVo.SEX_2 = "女";
                                }
                                #endregion

                                //胎次
                                chilVo.SEQUENCE = "1";
                                //出生时间
                                if (drMother["itemid"].ToString() == "dateTimePicker5")
                                    chilVo.DATEOFBIRTH = Function.Datetime(drMother["itemcontent"]).ToString("yyyyMMddHHmm");

                                #endregion
                            }
                            vo.lstChild.Add(chilVo);
                        }

                        lstMother.Add(vo);
                    }
                }

                if (lstMother != null && lstMother.Count > 0)
                {
                    string registerId = string.Empty;
                    //设置一个最大值
                    this.progressBarControl.Properties.Maximum = lstMother.Count;

                    this.progressBarControl.Visible = true;
                    this.progressBarControl.Position = 0;
                    foreach (EntityMother motherVo in lstMother)
                    {
                        #region xml.setvalue

                        StringBuilder xmlUpload = new StringBuilder();
                        xmlUpload.AppendLine("<?xml version=\"1.0\" encoding=\"GBK\" ?>");
                        xmlUpload.AppendLine("<Document type=\"Request Save\" versionNumber=\"\" value=\"1.0\">");
                        xmlUpload.AppendLine("<realmCode code=\"4419.CN\"/>");
                        xmlUpload.AppendLine("<code code=\"4419.A01.02.208\" codeSystem=\"4419.CN.01\" codeSystemName=\"东莞市妇幼卫生信息交互共享文档分类编码系统\"/>");
                        xmlUpload.AppendLine("<title>请求推送某孕产妇分娩信息</title>");
                        xmlUpload.AppendLine("<author>");
                        xmlUpload.AppendLine("<authorID code=\"763709818\" authorname=\"东莞市茶山医院\"/>");
                        xmlUpload.AppendLine("<InformationsystemID code=\"A74CC68F-B009-4264-A880-FBE87DD91E56\" InformationsystemName=\"东莞市茶山医院HIS管理系统\"/>");
                        xmlUpload.AppendLine(string.Format("<GenerationTime type=\"TS\" value=\"{0}\"/>", DateTime.Now.ToString("yyyyMMddHHmm")));
                        xmlUpload.AppendLine("</author>");
                        xmlUpload.AppendLine("<component>");
                        xmlUpload.AppendLine(string.Format("<OperationType value=\"{0}\"/>", motherVo.flagId == 0 ? "NEW" : "UPDATE"));
                        xmlUpload.AppendLine("<recordNumber value=\"1\" type=\"INT\"/>");
                        xmlUpload.AppendLine("<record>");
                        xmlUpload.AppendLine(string.Format("<HISID>{0}</HISID>", motherVo.HISID));                          // HIS系统唯一ID
                        xmlUpload.AppendLine(string.Format("<IDCARD>{0}</IDCARD>", motherVo.IDCARD));                       // 女方身份证号
                        xmlUpload.AppendLine(string.Format("<NAME>{0}</NAME>", motherVo.NAME));                             // 母亲姓名

                        xmlUpload.AppendLine(string.Format("<MATTER code=\"{0}\" codesystem=\"STD_ISSUEREASON\">{1}</MATTER>", motherVo.MATTER_1, motherVo.MATTER_2));                      // 签发原因代码; 签发原因（00：信息齐全(双亲)，02：信息不全(单亲)）
                        xmlUpload.AppendLine(string.Format("<BEDNO>{0}</BEDNO>", motherVo.BEDNO));                          // 床号
                        xmlUpload.AppendLine(string.Format("<ZYH>{0}</ZYH>", motherVo.ZYH));                                // 住院号
                        xmlUpload.AppendLine(string.Format("<INTIRE>{0}</INTIRE>", motherVo.INTIRE));                       // 当前第几胎
                        xmlUpload.AppendLine(string.Format("<INHOSPITALIZATIONIN>{0}</INHOSPITALIZATIONIN>", motherVo.INHOSPITALIZATIONIN));                                                // 当前第几次住院
                        xmlUpload.AppendLine(string.Format("<PLACETYPE code=\"{0}\" codesystem=\"STD_PLACETYPE\">{1}</PLACETYPE>", motherVo.PLACETYPE_1, motherVo.PLACETYPE_2));            // 分娩地点类型代码; 分娩地点类型名称
                        xmlUpload.AppendLine(string.Format("<CYESISWEEK>{0}</CYESISWEEK>", motherVo.CYESISWEEK));           // 分娩孕周(日)
                        xmlUpload.AppendLine(string.Format("<FETUSNUMBER code=\"{0}\" codesystem=\"STD_FETUSNUM\">{1}</FETUSNUMBER>", motherVo.FETUSNUMBER_1, motherVo.FETUSNUMBER_2));     // 胎数代码; 胎数
                        xmlUpload.AppendLine(string.Format("<TAIMOPOLIEFANGSHI code=\"{0}\" codesystem=\"STD_TAIMOPOLIE\">{1}</TAIMOPOLIEFANGSHI>", motherVo.TAIMOPOLIEFANGSHI_1, motherVo.TAIMOPOLIEFANGSHI_2));   // 胎膜破裂方式代码; 胎膜破裂方式名称
                        xmlUpload.AppendLine(string.Format("<TAIMOPOLIE>{0}</TAIMOPOLIE>", motherVo.TAIMOPOLIE));           // 胎膜破裂时间
                        xmlUpload.AppendLine(string.Format("<CHILDBIRTHTIME>{0}</CHILDBIRTHTIME>", motherVo.CHILDBIRTHTIME));                                                               // 分娩时间
                        xmlUpload.AppendLine(string.Format("<CHIBIRTYPE code=\"{0}\" codesystem=\"STD_CHIBIRTYPE\">{1}</CHIBIRTYPE>", motherVo.CHIBIRTYPE_1, motherVo.CHIBIRTYPE_2));       // 分娩方式代码; 分娩方式
                        xmlUpload.AppendLine(string.Format("<FETUSPOSITION code=\"{0}\" codesystem=\"STD_FETUSPOSITION\">{1}</FETUSPOSITION>", motherVo.FETUSPOSITION_1, motherVo.FETUSPOSITION_2));    // 胎方位代码; 胎方位
                        xmlUpload.AppendLine(string.Format("<ONELAYHOUR>{0}</ONELAYHOUR>", motherVo.ONELAYHOUR));           // 第一产程（小时）
                        xmlUpload.AppendLine(string.Format("<ONELAY>{0}</ONELAY>", motherVo.ONELAY));                       // 第一产程（分钟）
                        xmlUpload.AppendLine(string.Format("<TWOLAYHOUR>{0}</TWOLAYHOUR>", motherVo.TWOLAYHOUR));           // 第二产程（小时）
                        xmlUpload.AppendLine(string.Format("<TWOLAY>{0}</TWOLAY>", motherVo.TWOLAY));                       // 第二产程（分钟）
                        xmlUpload.AppendLine(string.Format("<THREELAYHOUR>{0}</THREELAYHOUR>", motherVo.THREELAYHOUR));     // 第三产程（小时）
                        xmlUpload.AppendLine(string.Format("<THREELAY>{0}</THREELAY>", motherVo.THREELAY));                 // 第三产程（分钟）
                        xmlUpload.AppendLine(string.Format("<ALLLAYHOUR>{0}</ALLLAYHOUR>", motherVo.ALLLAYHOUR));           // 总产程（小时）
                        xmlUpload.AppendLine(string.Format("<ALLLAY>{0}</ALLLAY>", motherVo.ALLLAY));                       // 总产程（分钟）
                        xmlUpload.AppendLine(string.Format("<PLACENTALTIME>{0}</PLACENTALTIME>", motherVo.PLACENTALTIME));  // 胎盘娩出时间
                        xmlUpload.AppendLine(string.Format("<PLACENTALFANGSHI code=\"{0}\"  codesystem=\"STD_PLACENTALFANGSHI\">{1}</PLACENTALFANGSHI>", motherVo.PLACENTALFANGSHI_1, motherVo.PLACENTALFANGSHI_2));    // 胎盘娩出方式代码; 胎盘娩出方式
                        xmlUpload.AppendLine(string.Format("<DELIVERYMEASURES>{0}</DELIVERYMEASURES>", motherVo.DELIVERYMEASURES));                                                         // 分娩措施
                        xmlUpload.AppendLine(string.Format("<TAIPAN code=\"{0}\" codesystem=\"STD_TAIPAN\">{1}</TAIPAN>", motherVo.TAIPAN_1, motherVo.TAIPAN_2));                           // 胎膜胎盘完整性代码; 胎盘完整性
                        xmlUpload.AppendLine(string.Format("<PLACENTA code=\"{0}\" codesystem=\"STD_PLACENTA\">{1}</PLACENTA>", motherVo.PLACENTA_1, motherVo.PLACENTA_2));                 // 胎膜完整性代码; 胎膜完整性
                        xmlUpload.AppendLine(string.Format("<JIDAI>{0}</JIDAI>", motherVo.JIDAI));                          // 脐带长度(单位：cm)
                        xmlUpload.AppendLine(string.Format("<LUCIDITY code=\"{0}\" codesystem=\"STD_LUCIDITY\">{1}</LUCIDITY>", motherVo.LUCIDITY_1, motherVo.LUCIDITY_2));                 // 羊水清否代码; 羊水清否
                        xmlUpload.AppendLine(string.Format("<DEGREE code=\"{0}\" codesystem=\"STD_DEGREE\">{1}</DEGREE>", motherVo.DEGREE_1, motherVo.DEGREE_2));                           // 羊水分度代码; 羊水分度
                        xmlUpload.AppendLine(string.Format("<AMNIOTIC>{0}</AMNIOTIC>", motherVo.AMNIOTIC));                 // 羊水量(单位：ml)
                        xmlUpload.AppendLine(string.Format("<PLACENTALLONG>{0}</PLACENTALLONG>", motherVo.PLACENTALLONG));                                                                  // 胎盘长（单位cm）
                        xmlUpload.AppendLine(string.Format("<PLACENTAWIDTH>{0}</PLACENTAWIDTH>", motherVo.PLACENTAWIDTH));                                                                  // 胎盘宽（单位cm）
                        xmlUpload.AppendLine(string.Format("<PLACENTALTHICKNESS>{0}</PLACENTALTHICKNESS>", motherVo.PLACENTALTHICKNESS));                                                   // 胎盘厚（单位cm）
                        xmlUpload.AppendLine(string.Format("<ISPERINEUMCUT code=\"{0}\" codesystem=\"STD_ISPERINEUMCUT\">{1}</ISPERINEUMCUT>", motherVo.ISPERINEUMCUT_1, motherVo.ISPERINEUMCUT_2));    // 会阴情况代码; 会阴情况
                        xmlUpload.AppendLine(string.Format("<SUTURESITUATION code=\"{0}\" codesystem=\"STD_SUTURESITUATION\">{1}</SUTURESITUATION>", motherVo.SUTURESITUATION_1, motherVo.SUTURESITUATION_2));  // 缝合情况代码; 缝合情况
                        xmlUpload.AppendLine(string.Format("<SEW>{0}</SEW>", motherVo.SEW));                                // 缝合针数(单位：针)
                        xmlUpload.AppendLine(string.Format("<OPERATIONREASON>{0}</OPERATIONREASON>", motherVo.OPERATIONREASON));// 手术原因
                        xmlUpload.AppendLine(string.Format("<CHUXUE>{0}</CHUXUE>", motherVo.CHUXUE));                       // 阴道分娩产后2h出血量（单位：ml）
                        xmlUpload.AppendLine(string.Format("<SSZXM>{0}</SSZXM>", motherVo.SSZXM));                          // 手术人
                        xmlUpload.AppendLine(string.Format("<ACCUSR>{0}</ACCUSR>", motherVo.ACCUSR));                       // 接生人
                        xmlUpload.AppendLine(string.Format("<OPERATEDATE>{0}</OPERATEDATE>", motherVo.OPERATEDATE));        // 录入时间
                        xmlUpload.AppendLine(string.Format("<ORG code=\"{0}\" codesystem=\"STD_ORGAN\">{1}</ORG>", motherVo.ORG_1, motherVo.ORG_2));                                        // 录入单位机构代码; 录入单位
                        foreach (EntityChild vo in motherVo.lstChild)
                        {
                            xmlUpload.AppendLine("<BABY>");
                            xmlUpload.AppendLine(string.Format("<BABYNAME>{0}</BABYNAME>", vo.BABYNAME));                   // 婴儿姓名
                            xmlUpload.AppendLine(string.Format("<SEX code=\"{0}\" codesystem=\"GB/T 2261.1\">{1}</SEX>", vo.SEX_1, vo.SEX_2));                                              // 婴儿性别代码; 婴儿性别
                            xmlUpload.AppendLine(string.Format("<SEQUENCE>{0}</SEQUENCE>", vo.SEQUENCE));                   // 胎次
                            xmlUpload.AppendLine(string.Format("<DATEOFBIRTH>{0}</DATEOFBIRTH>", vo.DATEOFBIRTH));          // 出生时间
                            xmlUpload.AppendLine(string.Format("<AVOIRDUPOIS>{0}</AVOIRDUPOIS>", vo.AVOIRDUPOIS));          // 体重
                            xmlUpload.AppendLine(string.Format("<STATURE>{0}</STATURE>", vo.STATURE));                      // 身长
                            xmlUpload.AppendLine(string.Format("<TOUWEI>{0}</TOUWEI>", vo.TOUWEI));                         // 头围
                            xmlUpload.AppendLine(string.Format("<ISBUG code=\"{0}\" codesystem=\"STD_ISBUG\">{1}</ISBUG>", vo.ISBUG_1, vo.ISBUG_2));                                        // 是否畸形代码; 是否畸形
                            xmlUpload.AppendLine(string.Format("<APGAR1>{0}</APGAR1>", vo.APGAR1));                         // 1min Apgar总分
                            xmlUpload.AppendLine(string.Format("<APGAR5>{0}</APGAR5>", vo.APGAR5));                         // 5min Apgar总分
                            xmlUpload.AppendLine(string.Format("<APGAR10>{0}</APGAR10>", vo.APGAR10));                      // 10min Apgar总分
                            xmlUpload.AppendLine(string.Format("<HBIGTIME code=\"{0}\" codesystem=\"STD_HBIGTIME\">{1}</HBIGTIME>", vo.HBIGTIME_1, vo.HBIGTIME_2));                         // 是否注射乙肝免疫球蛋白代码; 是否注射乙肝免疫球蛋白
                            xmlUpload.AppendLine(string.Format("<INJECTIONDATE>{0}</INJECTIONDATE>", vo.INJECTIONDATE));                                                                    // 注射日期
                            xmlUpload.AppendLine(string.Format("<JILIANG>{0}</JILIANG>", vo.JILIANG));                                                                                      // 注射剂量（单位：IU）
                            xmlUpload.AppendLine(string.Format("<SKINCONTACT code=\"{0}\" codesystem=\"STD_SKINCONTACT\">{1}</SKINCONTACT>", vo.SKINCONTACT_1, vo.SKINCONTACT_2));          // 产后30分钟内皮肤接触情况代码; 产后30分钟内皮肤接触情况
                            xmlUpload.AppendLine("</BABY>");
                        }
                        xmlUpload.AppendLine("</record>");
                        xmlUpload.AppendLine("</component>");
                        xmlUpload.AppendLine("</Document>");

                        #endregion

                        Log.Output("上传信息：" + Environment.NewLine + xmlUpload.ToString());
                        WebService ws = new WebService();
                        string res = ws.SaveInfoStringTypeXML("A74CC68F-B009-4264-A880-FBE87DD91E56", "763709818", xmlUpload.ToString());
                        Log.Output("返回信息：" + Environment.NewLine + res);

                        //ws.HelloWorld()

                        //ws.GetInfo()

                        MessageBox.Show(res);

                        // 处理当前消息队列中的所有windows消息
                        Application.DoEvents();
                        // 执行步长
                        this.progressBarControl.PerformStep();
                        // regId数组
                        registerId += motherVo.RegisterId + ",";
                    }

                    EntitySysTaskLog logVo = new EntitySysTaskLog();
                    logVo.typeId = "0006";
                    logVo.execTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    logVo.ipAddr = Function.LocalIP();
                    logVo.execStatus = 1;
                    logVo.execDesc = "上传成功 共 " + lstMother.Count + " 人 " + registerId.TrimEnd(',');
                    this.SaveTaskLog(logVo);
                }
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                this.progressBarControl.Visible = false;
                //this.RefreshTask();
                this.gvTask.ViewCaption = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint;
            }
        }
        #endregion

        #region 读取日志
        /// <summary>
        /// 读取日志
        /// </summary>
        /// <returns></returns>
        public List<EntitySysTaskLog> GetTaskLog()
        {
            List<EntitySysTaskLog> data = new List<EntitySysTaskLog>();
            SqlHelper svc = null;
            try
            {
                svc = new SqlHelper(EnumBiz.onlineDB);
                EntitySysTaskLog vo = new EntitySysTaskLog() { typeId = "0006" };
                data = EntityTools.ConvertToEntityList<EntitySysTaskLog>(svc.Select(vo, EntitySysTaskLog.Columns.typeId));
            }
            catch (Exception e)
            {
                ExceptionLog.OutPutException(e);
            }
            finally
            {
                svc = null;
            }
            return data;
        }
        #endregion

        #region 保存日志
        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="logVo"></param>
        /// <returns></returns>
        public int SaveTaskLog(EntitySysTaskLog logVo)
        {
            int affectRows = 0;
            SqlHelper svc = null;
            try
            {
                svc = new SqlHelper(EnumBiz.onlineDB);
                affectRows = svc.Commit(svc.GetInsertParm(logVo));
            }
            catch (Exception e)
            {
                ExceptionLog.OutPutException(e);
                affectRows = -1;
            }
            finally
            {
                svc = null;
            }
            return affectRows;
        }
        #endregion

        #region  获取孕产妇基本信息
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hisId"></param>
        /// <returns></returns>
        EntityMother GetPlatMotherInfo(string hisId)
        {
            EntityMother vo = null;
            #region xmlIn
            StringBuilder xmlIn = new StringBuilder();
            xmlIn.AppendLine("<?xml version=\"1.0\" encoding=\"GBK\" ?>");
            xmlIn.AppendLine("<Document type=\"Request Get\" versionNumber=\"\" value=\"1.0\">");
            xmlIn.AppendLine("<realmCode code=\"4419.CN\"/>");
            xmlIn.AppendLine("<code code=\"4419.A01.02.101\" codeSystem=\"4419.CN.01\" codeSystemName=\"东莞市妇幼卫生信息交互共享文档分类编码系统\"/>");
            xmlIn.AppendLine("<title>请求获取孕产妇基本信息</title>");
            xmlIn.AppendLine("<author>");
            xmlIn.AppendLine("<authorID code=\"763709818\" authorname=\"东莞市茶山医院\"/>");
            xmlIn.AppendLine("<InformationsystemID code=\"A74CC68F-B009-4264-A880-FBE87DD91E56\" InformationsystemName=\"东莞市茶山医院HIS管理系统\"/>");
            xmlIn.AppendLine(string.Format("<GenerationTime type=\"TS\" value=\"{0}\"/>", DateTime.Now.ToString("yyyyMMddHHmm")));
            xmlIn.AppendLine("</author>");
            xmlIn.AppendLine("<component>");
            xmlIn.AppendLine("<HDSB0101000></HDSB0101000>");
            xmlIn.AppendLine(string.Format("<HISID>{0}</HISID>", hisId));
            xmlIn.AppendLine("<HDSB0101001></HDSB0101001>");
            xmlIn.AppendLine("<HDSB0101004 code=\"\" codesystem=\"CV02.01.101\"></HDSB0101004>");
            xmlIn.AppendLine("<HDSB0101005></HDSB0101005>");
            xmlIn.AppendLine("<HDSB0101006></HDSB0101006>");
            xmlIn.AppendLine("</component>");
            xmlIn.AppendLine("</Document>");
            #endregion

            Log.Output("上传信息：" + Environment.NewLine + xmlIn);
            try
            {
                WebService ws = new WebService();
                string res = ws.GetInfoStringTypeXML("A74CC68F-B009-4264-A880-FBE87DD91E56", "763709818", xmlIn.ToString());
                Log.Output("返回信息：" + Environment.NewLine + res);

                if (!string.IsNullOrEmpty(res))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(res);
                    string affect = doc["Document"]["component"]["OperationSuccess"].InnerText;
                    if (affect == "YES")
                    {
                        XmlElement ele = doc["Document"]["component"]["record"];
                        string stdIsEnd = string.Empty;
                        if (ele != null)
                        {
                            stdIsEnd = ele["HDSB0101036"].InnerText;
                           // if (stdIsEnd == "是")
                                //return null; 
                            vo = new EntityMother();
                            vo.HISID = ele["HISID"].InnerText;
                            vo.NAME = ele["HDSB0101001"].InnerText;
                            vo.IDCARD = ele["HDSB0101005"].InnerText;
                            vo.BARCODE = ele["HDSB0101000"].InnerText;
                            vo.HDSB0101026 = ele["HDSB0101026"].InnerText;
                            vo.HDSB0101030_2 = ele["HDSB0101030"].InnerText;
                            vo.HDSB0101021 = ele["HDSB0101006"].InnerText;
                            vo.HDSB0101034 = ele["HDSB0101034"].InnerText;
                            vo.HDSB0101035 = ele["HDSB0101035"].InnerText;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionLog.OutPutException(e);
            }

            return vo;
        }
        #endregion

        #region 上传孕产妇辅助检查
        /// <summary>
        /// 
        /// </summary>
        void UploadAssistant(string upDate,string cardNo)
        {
            try
            {
                List<EntityWacCheckRecord> lstWacCheck = new List<EntityWacCheckRecord>();
                EntityWacCheckRecord wacCheckRecordVo = null;
                DataTable dtResult = null;
                DataTable dtWacItem = null;
                string Sql = string.Empty;
                string Sql1 = string.Empty;
                string deptId = string.Empty;
                string deptName = string.Empty;
                string checktor = string.Empty;
                string checkDate = string.Empty;
                string applicationId = string.Empty;
                string hisGroupId = string.Empty;
                string applyunitid = string.Empty;
                string applyunitname = string.Empty;
                string applyunitidLast = string.Empty;
                string itemname = string.Empty;
                string assistantCode = string.Empty;
                string assistantName = string.Empty;
                string assistantStr = string.Empty;
                string resultStr = string.Empty;
                string recordStr = string.Empty;
                string upStr = string.Empty;
                decimal upLoadCount = 0;
                IDataParameter[] parm = null;
                SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);
                string todayStr = null;

                if (string.IsNullOrEmpty(upDate))
                    todayStr = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                else
                    todayStr = upDate;

                Sql = @"select * from t_def_wacitemrecord a 
                                            where a.uploaddate between to_date(?, 'yyyy-mm-dd hh24:mi:ss') 
                                            and to_date(?, 'yyyy-mm-dd hh24:mi:ss')";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtUp = svc.GetDataTable(Sql, parm);

                Sql = @"select distinct d.patientid_chr as patientid
                              from t_opr_lis_sample d
                              left join t_bse_deptdesc dept
                                on d.appl_deptid_chr = dept.deptid_chr
                              left join t_bse_patientcard card
                                on d.patientid_chr = card.patientid_chr
                             where d.status_int > 5
                               and dept.deptid_chr in ('0000370', '0000225','0000226','0000222')
                               and d.confirm_dat between
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss') ";

                if (!string.IsNullOrEmpty(cardNo))
                {
                    Sql += " and card.patientcardid_chr = '" + cardNo.Trim() + "'";
                }

                Sql += Environment.NewLine + "order by d.patientid_chr";

                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtPat = svc.GetDataTable(Sql, parm);
                if (dtPat != null && dtPat.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtPat.Rows)
                    {
                        string hisId = dr["patientid"].ToString();
                        EntityMother motherVo = null;

                        // 查找平台记录
                        motherVo = GetPlatMotherInfo(hisId);

                        if (motherVo != null)
                        {
                            #region 查找检验结果
                            if (!string.IsNullOrEmpty(hisId))
                            {
                                Sql = @"select d.application_id_chr    as applicationid,
                                               t.apply_unit_id_chr     as applyunitid,
                                               t1.apply_unit_name_vchr as applyunitname,
                                               d.patientid_chr         as patientid,
                                               dept.deptid_chr,
                                               dept.deptname_vchr,
                                               r1.check_item_id_chr    as itemid,
                                               r1.CHECK_ITEM_NAME_VCHR as itemname,
                                               r1.result_vchr          as result,
                                               e.lastname_vchr        as checktor,
                                               d.confirm_dat       as checkdate
                                          from t_opr_lis_sample d
                                          left join t_opr_lis_check_result r1
                                            on d.sample_id_chr = r1.sample_id_chr
                                          left join t_opr_lis_app_apply_unit t
                                            on d.application_id_chr = t.application_id_chr
                                          left join t_aid_lis_apply_unit t1
                                            on t.apply_unit_id_chr = t1.apply_unit_id_chr
                                          left join t_bse_deptdesc dept
                                            on d.appl_deptid_chr = dept.deptid_chr
                                          left join t_bse_employee e
                                            on r1.operator_id_chr = e.empid_chr
                                         where d.status_int > 5
                                           and d.confirm_dat between
                                       to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                                       to_date(?, 'yyyy-mm-dd hh24:mi:ss')  
                                       and d.patientid_chr <> '-1' ";

                                Sql += " and d.patientid_chr = '" + hisId + "'" + Environment.NewLine;
                                Sql += "order by d.application_id_chr";
                                parm = svc.CreateParm(2);
                                parm[0].Value = todayStr + " 00:00:00";
                                parm[1].Value = todayStr + " 23:59:59";

                                dtResult = svc.GetDataTable(Sql, parm);
                            }
                            #endregion

                            if (dtResult != null && dtResult.Rows.Count > 0)
                            {
                                applyunitidLast = "";
                                upLoadCount++;
                                DataRow[] drr = dtResult.Select("patientid = '" + motherVo.HISID + "'", "applyunitid desc");
                                upStr += motherVo.HISID + ":";
                                if (drr != null && drr.Length > 0)
                                {
                                    for (int drI = 0; drI < drr.Length; drI++)
                                    {
                                        applyunitid = drr[drI]["applyunitid"].ToString();

                                        hisGroupId = drr[drI]["applyunitid"].ToString();
                                        deptId = drr[drI]["deptid_chr"].ToString();
                                        deptName = drr[drI]["deptname_vchr"].ToString();
                                        checktor = drr[drI]["checktor"].ToString();
                                        checkDate = drr[drI]["checkDate"].ToString();
                                        applicationId = drr[drI]["applicationid"].ToString();

                                        if (applyunitid != applyunitidLast)
                                        {
                                            Sql = @"select a.platgroupid,
                                                       a.platgroupname,
                                                       a.hisgroupid,
                                                       a.platitemid,
                                                       a.platitemname,
                                                       a.hisitemid
                                                  from t_def_wacitem a 
                                                  where a.hisgroupid  = ?  order by a.platgroupid";

                                            applyunitidLast = applyunitid;
                                            parm = svc.CreateParm(1);
                                            parm[0].Value = hisGroupId;
                                            dtWacItem = svc.GetDataTable(Sql, parm);

                                            if (dtWacItem != null && dtWacItem.Rows.Count > 0)
                                            {
                                                upStr += hisGroupId + "、";

                                                if (dtUp != null && dtUp.Rows.Count > 0)
                                                {
                                                    DataRow[] drrUp = dtUp.Select("patientid = '" + motherVo.HISID + "' and hisgroupid = '" + hisGroupId + "' and applicationId = '" + applicationId + "'");
                                                    motherVo.flagId = ((drrUp != null && drrUp.Length > 0) ? 1 : 0);
                                                }
                                                else
                                                {
                                                    motherVo.flagId = 0;
                                                }

                                                string xmlUpload = string.Empty;
                                                assistantStr = "";
                                                xmlUpload += "<?xml version=\"1.0\" encoding=\"GBK\" ?>" + Environment.NewLine;
                                                xmlUpload += "<Document type=\"Request Save\" versionNumber=\"\" value=\"1.0\">" + Environment.NewLine;
                                                xmlUpload += "<realmCode code=\"4419.CN\"/>" + Environment.NewLine;
                                                xmlUpload += "<code code=\"4419.A01.02.211\" codeSystem=\"4419.CN.01\" codeSystemName=\"东莞市妇幼卫生信息交互共享文档分类编码系统\"/>" + Environment.NewLine;
                                                xmlUpload += "<title>请求推送某产妇的辅助检查信息</title>" + Environment.NewLine;
                                                xmlUpload += "<author>" + Environment.NewLine;
                                                xmlUpload += "<authorID code=\"763709818\" authorname=\"东莞市茶山医院\"/>" + Environment.NewLine;
                                                xmlUpload += "<InformationsystemID code=\"A74CC68F-B009-4264-A880-FBE87DD91E56\" InformationsystemName=\"东莞市茶山医院HIS管理系统\"/>" + Environment.NewLine;
                                                xmlUpload += string.Format("<GenerationTime type=\"TS\" value=\"{0}\"/>", DateTime.Now.ToString("yyyyMMddHHmm")) + Environment.NewLine;
                                                xmlUpload += "</author>" + Environment.NewLine;
                                                xmlUpload += "<component>" + Environment.NewLine;
                                                xmlUpload += string.Format("<OperationType value=\"{0}\"/>", motherVo.flagId == 0 ? "NEW" : "UPDATE");
                                xmlUpload += "{0}" + Environment.NewLine;
                                                xmlUpload += "</component>" + Environment.NewLine;
                                                xmlUpload += "</Document>" + Environment.NewLine;

                                                recordStr = string.Format("<recordNumber value=\"{0}\" type=\"INT\"/>", 1) + Environment.NewLine;
                                                recordStr += "<record>" + Environment.NewLine;
                                                                                                recordStr += string.Format("<HISID>{0}</HISID>", motherVo.HISID) + Environment.NewLine;
                                                recordStr += string.Format("<NAME>{0}</NAME>", motherVo.NAME) + Environment.NewLine;
                                                recordStr += string.Format("<BARCODE>{0}</BARCODE>", motherVo.BARCODE) + Environment.NewLine;
                                                recordStr += string.Format("<IDCARD>{0}</IDCARD>", motherVo.IDCARD) + Environment.NewLine;
                                                recordStr += "{0}" + Environment.NewLine;
                                                string assistantCodeLast = string.Empty;

                                                foreach (DataRow drItem in dtWacItem.Rows)
                                                {
                                                    assistantCode = drItem["platgroupid"].ToString();
                                                    assistantName = drItem["platgroupname"].ToString();
                                                    
                                                    if (assistantCode != assistantCodeLast)
                                                    {
                                                        assistantStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", assistantCode, assistantName) + Environment.NewLine;
                                                        assistantStr += string.Format("<APPID>{0}</APPID>", applicationId) + Environment.NewLine;
                                                        assistantStr += string.Format("<ASSISTANTNAME>{0}</ASSISTANTNAME>", assistantName) + Environment.NewLine;
                                                        assistantStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDEP code=\"{0}\" codesystem=\"STD_KESHI\">{1}</CHKDEP>", "25043", "妇产科") + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDATE>{0}</CHKDATE>", Function.Datetime(checkDate).ToString("yyyy-MM-dd")) + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", checktor) + Environment.NewLine;

                                                        assistantCodeLast = assistantCode;
                                                        DataRow[] drrAssist = dtWacItem.Select("platgroupid = '" + assistantCode + "'");
                                                        #region
                                                        if (drrAssist != null & drrAssist.Length > 0)
                                                        {
                                                            for (int drA = 0; drA < drrAssist.Length; drA++)
                                                            {
                                                                string platitemid = drrAssist[drA]["platitemid"].ToString();
                                                                string platitemname = drrAssist[drA]["platitemname"].ToString();
                                                                string hisitemid = drrAssist[drA]["hisitemid"].ToString();

                                                                assistantCode = drrAssist[drA]["platgroupid"].ToString();
                                                                assistantName = drrAssist[drA]["platgroupname"].ToString();
                                                                string result = string.Empty;

                                                                DataRow[] drrItem = dtResult.Select("applyunitid = '" + hisGroupId + "' and itemid = '" + hisitemid + "'");
                                                                if (drrItem != null && drrItem.Length > 0)
                                                                {
                                                                    DataRow drrR = drrItem[0];
                                                                    #region 上传记录
                                                                    wacCheckRecordVo = new EntityWacCheckRecord();
                                                                    wacCheckRecordVo.patientId = motherVo.HISID;
                                                                    wacCheckRecordVo.applicationId = applicationId;
                                                                    wacCheckRecordVo.platgroupid = assistantCode;
                                                                    wacCheckRecordVo.platgroupname = assistantName;
                                                                    wacCheckRecordVo.hisgroupid = hisGroupId;
                                                                    wacCheckRecordVo.hisgroupname = drrR["applyunitname"].ToString();
                                                                    wacCheckRecordVo.platitemid = platitemid;
                                                                    wacCheckRecordVo.platitemname = platitemname;
                                                                    wacCheckRecordVo.hisitemid = drrR["itemid"].ToString();
                                                                    wacCheckRecordVo.hisitemname = drrR["itemname"].ToString();
                                                                    wacCheckRecordVo.uploaddate = DateTime.Now;
                                                                    result = drrR["result"].ToString().Trim();
                                                                    if (platitemid == "26" || platitemid == "50")//ABO血型
                                                                    {
                                                                        result = result.Replace("型", "").Replace("型", "");
                                                                    }

                                                                    if (platitemid == "27" || platitemid == "51")//	Rh血型
                                                                    {
                                                                        if (result.Contains("阳"))
                                                                            result = "+";
                                                                        else if (result.Contains("阴"))
                                                                            result = "-";
                                                                    }

                                                                    if (platitemid == "20" || platitemid == "21" || platitemid == "22"
                                                                        || platitemid == "23" || platitemid == "24" || platitemid == "25"
                                                                        || platitemid == "28" || platitemid == "136" || platitemid == "137"
                                                                        || platitemid == "138" || platitemid == "139" || platitemid == "140"
                                                                        || platitemid == "5183" || platitemid == "5184" || platitemid == "5244"
                                                                        || platitemid == "5245" || platitemid == "5246" || platitemid == "5185"
                                                                        || platitemid == "5247" || platitemid == "5186" || platitemid == "5187"
                                                                        || platitemid == "5188" || platitemid == "5194")
                                                                    {
                                                                        if (result.Contains("±"))
                                                                        {
                                                                            result = "±";
                                                                        }
                                                                        else if (result.Contains("阳"))
                                                                        {
                                                                            result = "+";
                                                                        }
                                                                        else if (result.Contains("阴"))
                                                                        {
                                                                            result = "-";
                                                                        }
                                                                    }

                                                                    if (platitemid == "37" || platitemid == "38" || platitemid == "39"
                                                                        || platitemid == "40" || platitemid == "41" || platitemid == "30"
                                                                        || platitemid == "31" || platitemid == "32" || platitemid == "42")
                                                                    {
                                                                        if (result.Contains("阴") || result.Contains("-"))
                                                                        {
                                                                            result = "阴性";
                                                                        }
                                                                        else if (result.Contains("阳") || result.Contains("+"))
                                                                        {
                                                                            result = "阳性";
                                                                        }
                                                                    }

                                                                    if (platitemid == "150")
                                                                    {
                                                                        if (result.Contains("I"))
                                                                            result = "I度";
                                                                        else if (result.Contains("II"))
                                                                            result = "II度";
                                                                        else if (result.Contains("III"))
                                                                            result = "III度";
                                                                        else if (result.Contains("IV"))
                                                                            result = "IV度";
                                                                    }

                                                                    wacCheckRecordVo.result = result;
                                                                    lstWacCheck.Add(wacCheckRecordVo);
                                                                    #endregion
                                                                    assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", platitemid) + Environment.NewLine;
                                                                    assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", platitemname) + Environment.NewLine;
                                                                    assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", result) + Environment.NewLine;
                                                                    assistantStr += "</RESULT>" + Environment.NewLine;
                                                                }
                                                                else
                                                                {
                                                                    continue;
                                                                }
                                                            }
                                                        }

                                                        if (assistantCode == "311")
                                                        {
                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5243) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "是否初诊") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "是") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5189) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "是否拒检") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "否") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5193) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "孕期初次接受艾滋病检测相关告知或咨询") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "是") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5190) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "检测机构") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "东莞市茶山医院") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5192) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "检测日期") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", Function.Datetime(checkDate).ToString("yyyy-MM-dd")) + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;
                                                        }
                                                        #endregion
                                                        assistantStr += "</ASSISTANT>" + Environment.NewLine;
                                                    }
                                                }


                                               
                                                string consultStr = getConsult(motherVo);
                                                #region 咨询项目
                                                if (!string.IsNullOrEmpty(consultStr))
                                                    assistantStr += consultStr;
                                                #endregion

                                                recordStr += "</record>" + Environment.NewLine;
                                                recordStr = string.Format(recordStr, assistantStr);

                                                xmlUpload = string.Format(xmlUpload, recordStr);

                                                Log.Output("上传信息：" + Environment.NewLine + xmlUpload);

                                                WebService ws = new WebService();
                                                string res = ws.SaveInfoStringTypeXML("A74CC68F-B009-4264-A880-FBE87DD91E56", "763709818", xmlUpload);
                                                Log.Output("返回信息：" + Environment.NewLine + res);

                                                #region 保存上传记录

                                                XmlDocument doc = new XmlDocument();
                                                doc.LoadXml(res);
                                                string affect = doc["Document"]["component"]["OperationSuccess"].Attributes["value"].Value;
                                                if (affect != "YES")
                                                {
                                                    continue;
                                                }
                                                insertConsult(motherVo);
                                                List<DacParm> lstParm = new List<DacParm>();
                                                if (lstWacCheck.Count > 0)
                                                {
                                                    try
                                                    {
                                                        Sql = @"delete from t_def_wacitemrecord where applicationid = ? and hisgroupid = ? and hisitemid = ?  and platgroupid = ?";
                                                        Sql1 = @"insert into t_def_wacitemrecord values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                                                        foreach (EntityWacCheckRecord vo in lstWacCheck)
                                                        {
                                                            parm = svc.CreateParm(4);
                                                            parm[0].Value = vo.applicationId;
                                                            parm[1].Value = vo.hisgroupid;
                                                            parm[2].Value = vo.hisitemid;
                                                            parm[3].Value = vo.platgroupid;
                                                            lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql, parm));
                                                            //svc.ExecSql(Sql, parm);

                                                            parm = svc.CreateParm(12);
                                                            parm[0].Value = vo.patientId;
                                                            parm[1].Value = vo.applicationId;
                                                            parm[2].Value = vo.platgroupid;
                                                            parm[3].Value = vo.platgroupname;
                                                            parm[4].Value = vo.hisgroupid;
                                                            parm[5].Value = vo.hisgroupname;
                                                            parm[6].Value = vo.platitemid;
                                                            parm[7].Value = vo.platitemname;
                                                            parm[8].Value = vo.hisitemid;
                                                            parm[9].Value = vo.hisitemname;
                                                            parm[10].Value = vo.uploaddate = DateTime.Now;
                                                            parm[11].Value = vo.result;
                                                            //svc.ExecSql(Sql1, parm);
                                                            lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql1, parm));
                                                        }

                                                        if (lstParm.Count > 0)
                                                        {
                                                            svc.Commit(lstParm);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ExceptionLog.OutPutException(e);
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(upStr))
                    {
                        upStr = upStr.TrimEnd('、');
                        EntitySysTaskLog logVo = new EntitySysTaskLog();
                        logVo.typeId = "0005";
                        logVo.execTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        logVo.ipAddr = Function.LocalIP();
                        logVo.execStatus = 1;
                        logVo.execDesc = "上传成功 共 " + upLoadCount + " 人 " + upStr.TrimEnd(',');
                        this.SaveTaskLog(logVo);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                //this.progressBarControl.Visible = false;
                //this.RefreshTask();
                //this.gvTask.ViewCaption = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint;
            }
        }
        #endregion

        #region 补上传
        /// <summary>
        /// 
        /// </summary>
        void UploadAssistant2()
        {
            try
            {
                List<EntityWacCheckRecord> lstWacCheck = new List<EntityWacCheckRecord>();
                EntityWacCheckRecord wacCheckRecordVo = null;
                DataTable dtResult = null;
                DataTable dtWacItem = null;
                string Sql = string.Empty;
                string Sql1 = string.Empty;
                string deptId = string.Empty;
                string deptName = string.Empty;
                string checktor = string.Empty;
                string checkDate = string.Empty;
                string applicationId = string.Empty;
                string hisGroupId = string.Empty;
                string applyunitid = string.Empty;
                string applyunitname = string.Empty;
                string applyunitidLast = string.Empty;
                string itemname = string.Empty;
                string assistantCode = string.Empty;
                string assistantName = string.Empty;
                string assistantStr = string.Empty;
                string resultStr = string.Empty;
                string recordStr = string.Empty;
                string upStr = string.Empty;
                decimal upLoadCount = 0;
                IDataParameter[] parm = null;
                SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);
                string todayStr = "2019-10-01";
                string todayStr2 = "2019-10-31";

                Sql = @"select * from t_def_wacitemrecord a 
                                            where a.uploaddate between to_date(?, 'yyyy-mm-dd hh24:mi:ss') 
                                            and to_date(?, 'yyyy-mm-dd hh24:mi:ss')";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr2 + " 23:59:59";
                DataTable dtUp = svc.GetDataTable(Sql, parm);

                Sql = @"select distinct d.patientid_chr as patientid,d.patient_name_vchr,d.modify_dat
                              from t_opr_lis_sample d
                              left join t_bse_deptdesc dept
                                on d.appl_deptid_chr = dept.deptid_chr
                              left join t_bse_patientcard card
                                on d.patientid_chr = card.patientid_chr
                                left join t_opr_lis_app_apply_unit t
                                 on d.application_id_chr = t.application_id_chr
                             where d.status_int > 5
                               and dept.deptid_chr in ('0000370', '0000225','0000226','0000222')
                               and t.apply_unit_id_chr in ('001382',  '001177')
                               and d.patientid_chr = '0001695510'
                               and d.confirm_dat between
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss') ";


                Sql += Environment.NewLine + "order by d.patientid_chr";

                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr2 + " 23:59:59";
                DataTable dtPat = svc.GetDataTable(Sql, parm);
                if (dtPat != null && dtPat.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtPat.Rows)
                    {
                        string hisId = dr["patientid"].ToString();
                        EntityMother motherVo = null;

                        // 查找平台记录
                        motherVo = GetPlatMotherInfo(hisId);

                        if (motherVo != null)
                        {
                            #region 查找检验结果
                            if (!string.IsNullOrEmpty(hisId))
                            {
                                Sql = @"select d.application_id_chr    as applicationid,
                                               t.apply_unit_id_chr     as applyunitid,
                                               t1.apply_unit_name_vchr as applyunitname,
                                               d.patientid_chr         as patientid,
                                               dept.deptid_chr,
                                               dept.deptname_vchr,
                                               r1.check_item_id_chr    as itemid,
                                               r1.CHECK_ITEM_NAME_VCHR as itemname,
                                               r1.result_vchr          as result,
                                               e.lastname_vchr        as checktor,
                                               d.confirm_dat       as checkdate
                                          from t_opr_lis_sample d
                                          left join t_opr_lis_check_result r1
                                            on d.sample_id_chr = r1.sample_id_chr
                                          left join t_opr_lis_app_apply_unit t
                                            on d.application_id_chr = t.application_id_chr
                                          left join t_aid_lis_apply_unit t1
                                            on t.apply_unit_id_chr = t1.apply_unit_id_chr
                                          left join t_bse_deptdesc dept
                                            on d.appl_deptid_chr = dept.deptid_chr
                                          left join t_bse_employee e
                                            on r1.operator_id_chr = e.empid_chr
                                         where d.status_int > 5    
                                           and t.apply_unit_id_chr in ('001382',  '001177')
                                           and d.confirm_dat between
                                       to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                                       to_date(?, 'yyyy-mm-dd hh24:mi:ss')  
                                       and d.patientid_chr <> '-1' ";

                                Sql += " and d.patientid_chr = '" + hisId + "'" + Environment.NewLine;
                                Sql += "order by d.application_id_chr";
                                parm = svc.CreateParm(2);
                                parm[0].Value = todayStr + " 00:00:00";
                                parm[1].Value = todayStr2 + " 23:59:59";

                                dtResult = svc.GetDataTable(Sql, parm);
                            }
                            #endregion

                            if (dtResult != null && dtResult.Rows.Count > 0)
                            {
                                applyunitidLast = "";
                                upLoadCount++;
                                DataRow[] drr = dtResult.Select("patientid = '" + motherVo.HISID + "'", "applyunitid desc");
                                upStr += motherVo.HISID + ":";
                                if (drr != null && drr.Length > 0)
                                {
                                    for (int drI = 0; drI < drr.Length; drI++)
                                    {
                                        applyunitid = drr[drI]["applyunitid"].ToString();

                                        hisGroupId = drr[drI]["applyunitid"].ToString();
                                        deptId = drr[drI]["deptid_chr"].ToString();
                                        deptName = drr[drI]["deptname_vchr"].ToString();
                                        checktor = drr[drI]["checktor"].ToString();
                                        checkDate = drr[drI]["checkDate"].ToString();
                                        applicationId = drr[drI]["applicationid"].ToString();

                                        if (applyunitid != applyunitidLast)
                                        {
                                            Sql = @"select a.platgroupid,
                                                       a.platgroupname,
                                                       a.hisgroupid,
                                                       a.platitemid,
                                                       a.platitemname,
                                                       a.hisitemid
                                                  from t_def_wacitem a 
                                                  where a.hisgroupid  = ?  order by a.platgroupid";

                                            applyunitidLast = applyunitid;
                                            parm = svc.CreateParm(1);
                                            parm[0].Value = hisGroupId;
                                            dtWacItem = svc.GetDataTable(Sql, parm);

                                            if (dtWacItem != null && dtWacItem.Rows.Count > 0)
                                            {
                                                upStr += hisGroupId + "、";

                                                if (dtUp != null && dtUp.Rows.Count > 0)
                                                {
                                                    DataRow[] drrUp = dtUp.Select("patientid = '" + motherVo.HISID + "' and hisgroupid = '" + hisGroupId + "' and applicationId = '" + applicationId + "'");
                                                    motherVo.flagId = ((drrUp != null && drrUp.Length > 0) ? 1 : 0);
                                                }
                                                else
                                                {
                                                    motherVo.flagId = 0;
                                                }

                                                string xmlUpload = string.Empty;
                                                assistantStr = "";
                                                xmlUpload += "<?xml version=\"1.0\" encoding=\"GBK\" ?>" + Environment.NewLine;
                                                xmlUpload += "<Document type=\"Request Save\" versionNumber=\"\" value=\"1.0\">" + Environment.NewLine;
                                                xmlUpload += "<realmCode code=\"4419.CN\"/>" + Environment.NewLine;
                                                xmlUpload += "<code code=\"4419.A01.02.211\" codeSystem=\"4419.CN.01\" codeSystemName=\"东莞市妇幼卫生信息交互共享文档分类编码系统\"/>" + Environment.NewLine;
                                                xmlUpload += "<title>请求推送某产妇的辅助检查信息</title>" + Environment.NewLine;
                                                xmlUpload += "<author>" + Environment.NewLine;
                                                xmlUpload += "<authorID code=\"763709818\" authorname=\"东莞市茶山医院\"/>" + Environment.NewLine;
                                                xmlUpload += "<InformationsystemID code=\"A74CC68F-B009-4264-A880-FBE87DD91E56\" InformationsystemName=\"东莞市茶山医院HIS管理系统\"/>" + Environment.NewLine;
                                                xmlUpload += string.Format("<GenerationTime type=\"TS\" value=\"{0}\"/>", DateTime.Now.ToString("yyyyMMddHHmm")) + Environment.NewLine;
                                                xmlUpload += "</author>" + Environment.NewLine;
                                                xmlUpload += "<component>" + Environment.NewLine;
                                                xmlUpload += string.Format("<OperationType value=\"{0}\"/>", motherVo.flagId == 0 ? "NEW" : "UPDATE");
                                                xmlUpload += "{0}" + Environment.NewLine;
                                                xmlUpload += "</component>" + Environment.NewLine;
                                                xmlUpload += "</Document>" + Environment.NewLine;

                                                recordStr = string.Format("<recordNumber value=\"{0}\" type=\"INT\"/>", 1) + Environment.NewLine;
                                                recordStr += "<record>" + Environment.NewLine;
                                                recordStr += string.Format("<HISID>{0}</HISID>", motherVo.HISID) + Environment.NewLine;
                                                recordStr += string.Format("<NAME>{0}</NAME>", motherVo.NAME) + Environment.NewLine;
                                                recordStr += string.Format("<BARCODE>{0}</BARCODE>", motherVo.BARCODE) + Environment.NewLine;
                                                recordStr += string.Format("<IDCARD>{0}</IDCARD>", motherVo.IDCARD) + Environment.NewLine;
                                                recordStr += "{0}" + Environment.NewLine;
                                                string assistantCodeLast = string.Empty;

                                                foreach (DataRow drItem in dtWacItem.Rows)
                                                {
                                                    assistantCode = drItem["platgroupid"].ToString();
                                                    assistantName = drItem["platgroupname"].ToString();

                                                    if (assistantCode != assistantCodeLast)
                                                    {
                                                        assistantStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", assistantCode, assistantName) + Environment.NewLine;
                                                        assistantStr += string.Format("<APPID>{0}</APPID>", applicationId) + Environment.NewLine;
                                                        assistantStr += string.Format("<ASSISTANTNAME>{0}</ASSISTANTNAME>", assistantName) + Environment.NewLine;
                                                        assistantStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDEP code=\"{0}\" codesystem=\"STD_KESHI\">{1}</CHKDEP>", "25043", "妇产科") + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDATE>{0}</CHKDATE>", Function.Datetime(checkDate).ToString("yyyy-MM-dd")) + Environment.NewLine;
                                                        assistantStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", checktor) + Environment.NewLine;

                                                        assistantCodeLast = assistantCode;
                                                        DataRow[] drrAssist = dtWacItem.Select("platgroupid = '" + assistantCode + "'");
                                                        #region
                                                        if (drrAssist != null & drrAssist.Length > 0)
                                                        {
                                                            for (int drA = 0; drA < drrAssist.Length; drA++)
                                                            {
                                                                string platitemid = drrAssist[drA]["platitemid"].ToString();
                                                                string platitemname = drrAssist[drA]["platitemname"].ToString();
                                                                string hisitemid = drrAssist[drA]["hisitemid"].ToString();

                                                                assistantCode = drrAssist[drA]["platgroupid"].ToString();
                                                                assistantName = drrAssist[drA]["platgroupname"].ToString();
                                                                string result = string.Empty;

                                                                DataRow[] drrItem = dtResult.Select("applyunitid = '" + hisGroupId + "' and itemid = '" + hisitemid + "'");
                                                                if (drrItem != null && drrItem.Length > 0)
                                                                {
                                                                    DataRow drrR = drrItem[0];
                                                                    #region 上传记录
                                                                    wacCheckRecordVo = new EntityWacCheckRecord();
                                                                    wacCheckRecordVo.patientId = motherVo.HISID;
                                                                    wacCheckRecordVo.applicationId = applicationId;
                                                                    wacCheckRecordVo.platgroupid = assistantCode;
                                                                    wacCheckRecordVo.platgroupname = assistantName;
                                                                    wacCheckRecordVo.hisgroupid = hisGroupId;
                                                                    wacCheckRecordVo.hisgroupname = drrR["applyunitname"].ToString();
                                                                    wacCheckRecordVo.platitemid = platitemid;
                                                                    wacCheckRecordVo.platitemname = platitemname;
                                                                    wacCheckRecordVo.hisitemid = drrR["itemid"].ToString();
                                                                    wacCheckRecordVo.hisitemname = drrR["itemname"].ToString();
                                                                    wacCheckRecordVo.uploaddate = DateTime.Now;
                                                                    result = drrR["result"].ToString().Trim();
                                                                    if (platitemid == "26" || platitemid == "50")//ABO血型
                                                                    {
                                                                        result = result.Replace("型", "").Replace("型", "");
                                                                    }

                                                                    if (platitemid == "27" || platitemid == "51")//	Rh血型
                                                                    {
                                                                        if (result.Contains("阳"))
                                                                            result = "+";
                                                                        else if (result.Contains("阴"))
                                                                            result = "-";
                                                                    }

                                                                    if (platitemid == "20" || platitemid == "21" || platitemid == "22"
                                                                        || platitemid == "23" || platitemid == "24" || platitemid == "25"
                                                                        || platitemid == "28" || platitemid == "136" || platitemid == "137"
                                                                        || platitemid == "138" || platitemid == "139" || platitemid == "140"
                                                                        || platitemid == "5183" || platitemid == "5184" || platitemid == "5244"
                                                                        || platitemid == "5245" || platitemid == "5246" || platitemid == "5185"
                                                                        || platitemid == "5247" || platitemid == "5186" || platitemid == "5187"
                                                                        || platitemid == "5188" || platitemid == "5194")
                                                                    {
                                                                        if (result.Contains("±"))
                                                                        {
                                                                            result = "±";
                                                                        }
                                                                        else if (result.Contains("阳"))
                                                                        {
                                                                            result = "+";
                                                                        }
                                                                        else if (result.Contains("阴"))
                                                                        {
                                                                            result = "-";
                                                                        }
                                                                    }

                                                                    if (platitemid == "37" || platitemid == "38" || platitemid == "39"
                                                                        || platitemid == "40" || platitemid == "41" || platitemid == "30"
                                                                        || platitemid == "31" || platitemid == "32" || platitemid == "42")
                                                                    {
                                                                        if (result.Contains("阴") || result.Contains("-"))
                                                                        {
                                                                            result = "阴性";
                                                                        }
                                                                        else if (result.Contains("阳") || result.Contains("+"))
                                                                        {
                                                                            result = "阳性";
                                                                        }
                                                                    }

                                                                    if (platitemid == "150")
                                                                    {
                                                                        if (result.Contains("I"))
                                                                            result = "I度";
                                                                        else if (result.Contains("II"))
                                                                            result = "II度";
                                                                        else if (result.Contains("III"))
                                                                            result = "III度";
                                                                        else if (result.Contains("IV"))
                                                                            result = "IV度";
                                                                    }

                                                                    wacCheckRecordVo.result = result;
                                                                    lstWacCheck.Add(wacCheckRecordVo);
                                                                    #endregion
                                                                    assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", platitemid) + Environment.NewLine;
                                                                    assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", platitemname) + Environment.NewLine;
                                                                    assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", result) + Environment.NewLine;
                                                                    assistantStr += "</RESULT>" + Environment.NewLine;
                                                                }
                                                                else
                                                                {
                                                                    continue;
                                                                }
                                                            }
                                                        }

                                                        if (assistantCode == "311")
                                                        {
                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5243) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "是否初诊") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "是") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5189) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "是否拒检") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "否") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5193) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "孕期初次接受艾滋病检测相关告知或咨询") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "是") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5190) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "检测机构") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", "东莞市茶山医院") + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;

                                                            assistantStr += string.Format("<RESULT code=\"{0}\" codesystem=\"STD_RESULT\">", 5192) + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTNAME>{0}</RESULTNAME>", "检测日期") + Environment.NewLine;
                                                            assistantStr += string.Format("<RESULTVALUE>{0}</RESULTVALUE>", Function.Datetime(checkDate).ToString("yyyy-MM-dd")) + Environment.NewLine;
                                                            assistantStr += "</RESULT>" + Environment.NewLine;
                                                        }
                                                        #endregion
                                                        assistantStr += "</ASSISTANT>" + Environment.NewLine;
                                                    }
                                                }



                                                string consultStr = getConsult(motherVo);
                                                #region 咨询项目
                                                if (!string.IsNullOrEmpty(consultStr))
                                                    assistantStr += consultStr;
                                                #endregion

                                                recordStr += "</record>" + Environment.NewLine;
                                                recordStr = string.Format(recordStr, assistantStr);

                                                xmlUpload = string.Format(xmlUpload, recordStr);

                                                Log.Output("上传信息：" + Environment.NewLine + xmlUpload);

                                                WebService ws = new WebService();
                                                string res = ws.SaveInfoStringTypeXML("A74CC68F-B009-4264-A880-FBE87DD91E56", "763709818", xmlUpload);
                                                Log.Output("返回信息：" + Environment.NewLine + res);

                                                #region 保存上传记录

                                                XmlDocument doc = new XmlDocument();
                                                doc.LoadXml(res);
                                                string affect = doc["Document"]["component"]["OperationSuccess"].Attributes["value"].Value;
                                                if (affect != "YES")
                                                {
                                                    continue;
                                                }
                                                insertConsult(motherVo);
                                                List<DacParm> lstParm = new List<DacParm>();
                                                if (lstWacCheck.Count > 0)
                                                {
                                                    try
                                                    {
                                                        Sql = @"delete from t_def_wacitemrecord where applicationid = ? and hisgroupid = ? and hisitemid = ?  and platgroupid = ?";
                                                        Sql1 = @"insert into t_def_wacitemrecord values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                                                        foreach (EntityWacCheckRecord vo in lstWacCheck)
                                                        {
                                                            parm = svc.CreateParm(4);
                                                            parm[0].Value = vo.applicationId;
                                                            parm[1].Value = vo.hisgroupid;
                                                            parm[2].Value = vo.hisitemid;
                                                            parm[3].Value = vo.platgroupid;
                                                            lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql, parm));
                                                            //svc.ExecSql(Sql, parm);

                                                            parm = svc.CreateParm(12);
                                                            parm[0].Value = vo.patientId;
                                                            parm[1].Value = vo.applicationId;
                                                            parm[2].Value = vo.platgroupid;
                                                            parm[3].Value = vo.platgroupname;
                                                            parm[4].Value = vo.hisgroupid;
                                                            parm[5].Value = vo.hisgroupname;
                                                            parm[6].Value = vo.platitemid;
                                                            parm[7].Value = vo.platitemname;
                                                            parm[8].Value = vo.hisitemid;
                                                            parm[9].Value = vo.hisitemname;
                                                            parm[10].Value = vo.uploaddate = DateTime.Now;
                                                            parm[11].Value = vo.result;
                                                            //svc.ExecSql(Sql1, parm);
                                                            lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql1, parm));
                                                        }

                                                        if (lstParm.Count > 0)
                                                        {
                                                            svc.Commit(lstParm);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ExceptionLog.OutPutException(e);
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(upStr))
                    {
                        //upStr = upStr.TrimEnd('、');
                        //EntitySysTaskLog logVo = new EntitySysTaskLog();
                        //logVo.typeId = "0005";
                        //logVo.execTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        //logVo.ipAddr = Function.LocalIP();
                        //logVo.execStatus = 1;
                        //logVo.execDesc = "上传成功 共 " + upLoadCount + " 人 " + upStr.TrimEnd(',');
                        //this.SaveTaskLog(logVo);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                //this.progressBarControl.Visible = false;
                //this.RefreshTask();
                //this.gvTask.ViewCaption = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint;
            }
        }
        #endregion



        #region 上传孕产妇分娩信息
        /// <summary>
        /// 
        /// </summary>
        void UploadFMJL(string upDate)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                IDataParameter[] parm = null;
                SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);
                string registerId = string.Empty;
                decimal uploadCount = 0;
                string todayStr = null; 
                string Sql = string.Empty;
                string recordStr = string.Empty;
                Dictionary<string, string> dicKey = new Dictionary<string, string>();

                if (string.IsNullOrEmpty(upDate))
                    todayStr = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                else
                    todayStr = upDate;
                Sql = @"select b.registerid_chr,b.inpatientid_chr,b.inpatient_dat,b.patientid_chr, 
                               a.xmldata,b.inpatientcount_int, a.recorddate
                              from emrDataFMJLB a
                              left join t_opr_bih_register b
                                on a.registerid = b.registerid_chr 
                            where  a.recorddate between
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                                   to_date(?, 'yyyy-mm-dd hh24:mi:ss')
                             order by b.patientid_chr ";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtPat = svc.GetDataTable(Sql, parm);

                Sql = @"select a.registerid, a.inpatientid, a.inpatientdate, a.uploaddate
                          from t_opr_bih_wacrecord a
                         where (a.uploaddate between to_date(?, 'yyyy-mm-dd hh24:mi:ss') and
                               to_date(?, 'yyyy-mm-dd hh24:mi:ss'))";
                parm = svc.CreateParm(2);
                parm[0].Value = todayStr + " 00:00:00";
                parm[1].Value = todayStr + " 23:59:59";
                DataTable dtUp = svc.GetDataTable(Sql, parm);

                if (dtPat != null && dtPat.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtPat.Rows)
                    {
                        string hisId = dr["patientid_chr"].ToString();
                        EntityMother motherVo = null;
                        #region 查找平台记录
                        motherVo = GetPlatMotherInfo(hisId);
                        if (motherVo != null)
                        {
                            motherVo.RegisterId = dr["registerid_chr"].ToString();
                            motherVo.ipNo = dr["inpatientid_chr"].ToString();
                            motherVo.inpatientDate = dr["inpatient_dat"].ToString();

                            if (dtUp != null && dtUp.Rows.Count > 0)
                            {
                                DataRow[] drr = dtUp.Select("inpatientid = '" + motherVo.ipNo + "'");
                                motherVo.flagId = ((drr != null && drr.Length > 0) ? 1 : 0);
                            }
                            else
                            {
                                motherVo.flagId = 0;
                            }
                            string xmlUpload = string.Empty;
                            xmlUpload += "<?xml version=\"1.0\" encoding=\"GBK\" ?>" + Environment.NewLine;
                            xmlUpload += "<Document type=\"Request Save\" versionNumber=\"\" value=\"1.0\">" + Environment.NewLine;
                            xmlUpload += "<realmCode code=\"4419.CN\"/>" + Environment.NewLine;
                            xmlUpload += "<code code=\"4419.A01.02.208\" codeSystem=\"4419.CN.01\" codeSystemName=\"东莞市妇幼卫生信息交互共享文档分类编码系统\"/>" + Environment.NewLine;
                            xmlUpload += "<title>请求推送某孕产妇分娩信息</title>" + Environment.NewLine;
                            xmlUpload += "<author>" + Environment.NewLine;
                            xmlUpload += "<authorID code=\"763709818\" authorname=\"东莞市茶山医院\"/>" + Environment.NewLine;
                            xmlUpload += "<InformationsystemID code=\"A74CC68F-B009-4264-A880-FBE87DD91E56\" InformationsystemName=\"东莞市茶山医院HIS管理系统\"/>" + Environment.NewLine;
                            xmlUpload += string.Format("<GenerationTime type=\"TS\" value=\"{0}\"/>", DateTime.Now.ToString("yyyyMMddHHmm")) + Environment.NewLine;
                            xmlUpload += "</author>" + Environment.NewLine;
                            xmlUpload += "<component>" + Environment.NewLine;
                            xmlUpload += string.Format("<OperationType value=\"{0}\"/>", motherVo.flagId == 0 ? "NEW" : "UPDATE") + Environment.NewLine;
                            xmlUpload += "{0}" + Environment.NewLine;
                            xmlUpload += "</component>" + Environment.NewLine;
                            xmlUpload += "</Document>" + Environment.NewLine;

                            recordStr = string.Format("<recordNumber value=\"{0}\" type=\"INT\"/>", 1) + Environment.NewLine;

                            recordStr += "<record>" + Environment.NewLine;

                            string xmlData = dr["xmldata"].ToString();
                            dicKey = Function.ReadXmlNodes(xmlData, "FormData");

                            if (!string.IsNullOrEmpty(motherVo.HDSB0101026))
                            {
                                motherVo.MATTER_1 = "00";
                                motherVo.MATTER_2 = "信息齐全(双亲)";
                            }
                            else
                            {
                                motherVo.MATTER_1 = "02";
                                motherVo.MATTER_2 = "信息不全(单亲)";
                            }

                            motherVo.BEDNO = dicKey["PatientBedNo"];         // 床号
                            motherVo.ZYH = dicKey["PatientIpNo"];            // 住院号

                            //// 当前第几胎
                            //if (!string.IsNullOrEmpty(dicKey["INTIRE"]))
                            //    motherVo.INTIRE = (Function.Dec(dicKey["INTIRE"])).ToString();
                            //else
                            //    motherVo.INTIRE = "1";
                            motherVo.INHOSPITALIZATIONIN = dr["inpatientcount_int"].ToString();       // 当前第几次住院

                            //// 分娩地点类型代码; 分娩地点类型名称
                            //string PLACETYPE = dicKey["PLACETYPE"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(PLACETYPE) && PLACETYPE.Contains(" "))
                            //{
                            //    motherVo.PLACETYPE_1 = PLACETYPE.Split(' ')[0];
                            //    motherVo.PLACETYPE_2 = PLACETYPE.Split(' ')[1];
                            //}

                            motherVo.CYESISWEEK = dicKey["X003"];           // 分娩孕周(日)  

                            //胎数
                            string FETUSNUMBER = dicKey["FETUSNUMBER"].TrimStart().TrimEnd();
                            if (!string.IsNullOrEmpty(FETUSNUMBER) && FETUSNUMBER.Contains(" "))
                            {
                                motherVo.FETUSNUMBER_1 = FETUSNUMBER.Split(' ')[0];
                                motherVo.FETUSNUMBER_2 = FETUSNUMBER.Split(' ')[1];
                            }

                            // 胎膜破裂方式代码; 胎膜破裂方式名称
                            if (dicKey["A003"] == "1")
                            {
                                motherVo.TAIMOPOLIEFANGSHI_1 = "1";
                                motherVo.TAIMOPOLIEFANGSHI_2 = "人工";
                            }
                            else
                            {
                                motherVo.TAIMOPOLIEFANGSHI_1 = "0";
                                motherVo.TAIMOPOLIEFANGSHI_2 = "自然";
                            }

                            motherVo.TAIMOPOLIE = dicKey["X005"];           // 胎膜破裂时间

                            // 分娩方式代码; 分娩方式
                            if (dicKey["X010"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "1";
                                motherVo.CHIBIRTYPE_2 = "阴道自然分娩";
                            }
                            else if (dicKey["X011"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "23";
                                motherVo.CHIBIRTYPE_2 = "胎头吸引";
                            }
                            else if (dicKey["X012"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "21";
                                motherVo.CHIBIRTYPE_2 = "产钳助产";
                            }
                            else if (dicKey["X013"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "3";
                                motherVo.CHIBIRTYPE_2 = "剖宫产";
                            }
                            else if (dicKey["X014"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "22";
                                motherVo.CHIBIRTYPE_2 = "臀位助产";
                            }
                            else if (dicKey["X014"] == "1" || dicKey["X015"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "22";
                                motherVo.CHIBIRTYPE_2 = "臀位助产";
                            }
                            else if (dicKey["X016"] == "1")
                            {
                                motherVo.CHIBIRTYPE_1 = "34";
                                motherVo.CHIBIRTYPE_2 = "臀牵引";
                            }

                            //motherVo.OPERATEDATE = "201805172100";
                            //motherVo.ORG_1 = "4419060001";
                            //motherVo.ORG_2 = "东莞市茶山医院";
                            motherVo.CHILDBIRTHTIME = dicKey["X004"];// 分娩时间
                            //motherVo.OPERATEDATE = "2018-05-17";

                            motherVo.ONELAYHOUR = dicKey["X064"];         // 第一产程（小时）
                            motherVo.ONELAY = dicKey["X065"];                     // 第一产程（分钟）
                            motherVo.TWOLAYHOUR = dicKey["X066"];         // 第二产程（小时）
                            motherVo.TWOLAY = dicKey["X067"];                      // 第二产程（分钟）
                            motherVo.THREELAYHOUR = dicKey["X068"];    // 第三产程（小时）
                            motherVo.THREELAY = dicKey["X069"];                // 第三产程（分钟）
                            motherVo.ALLLAYHOUR = dicKey["X070"];          // 总产程（小时）
                            motherVo.ALLLAY = dicKey["X071"];                    // 总产程（分钟）
                            motherVo.PLACENTALTIME = dicKey["X018"]; // 胎盘娩出时间

                            //// 胎盘娩出方式代码; 胎盘娩出方式
                            //string PLACENTALFANGSHI = dicKey["PLACENTALFANGSHI"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(PLACENTALFANGSHI) && PLACENTALFANGSHI.Contains(" "))
                            //{
                            //    motherVo.PLACENTALFANGSHI_1 = PLACENTALFANGSHI.Split(' ')[0];
                            //    motherVo.PLACENTALFANGSHI_2 = PLACENTALFANGSHI.Split(' ')[1];
                            //}

                            motherVo.DELIVERYMEASURES = "";     // 分娩措施

                            // 胎膜胎盘完整性代码; 胎盘完整性
                            if (dicKey["X023"] == "1")
                            {
                                motherVo.TAIPAN_1 = "1";
                                motherVo.TAIPAN_2 = "完整";
                            }
                            else
                            {
                                motherVo.TAIPAN_1 = "2";
                                motherVo.TAIPAN_2 = "不完整";
                            }

                            // 胎膜完整性代码; 胎膜完整性
                            if (dicKey["X029"] == "1")
                            {
                                motherVo.PLACENTA_1 = "0";
                                motherVo.PLACENTA_2 = "完整";
                            }
                            else if (dicKey["X029"] == "1")
                            {
                                motherVo.PLACENTA_1 = "1";
                                motherVo.PLACENTA_2 = "不完整";
                            }
                            else
                            {
                                motherVo.PLACENTA_1 = "2";
                                motherVo.PLACENTA_2 = "其他";
                            }

                            motherVo.JIDAI = dicKey["X031"];       // 脐带长度(单位：cm)

                            //// 羊水清否代码; 羊水清否
                            //if (dicKey["LUCIDITY-Y"] == "1")
                            //{
                            //    motherVo.LUCIDITY_1 = "1";
                            //    motherVo.LUCIDITY_2 = "是";
                            //}
                            //else if (dicKey["LUCIDITY-N"] == "1")
                            //{
                            //    motherVo.LUCIDITY_1 = "0";
                            //    motherVo.LUCIDITY_2 = "否";
                            //}

                            //// 羊水分度代码; 羊水分度
                            //string DEGREE = dicKey["DEGREE"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(DEGREE) && DEGREE.Contains(" "))
                            //{
                            //    motherVo.DEGREE_1 = DEGREE.Split(' ')[0];
                            //    motherVo.DEGREE_2 = DEGREE.Split(' ')[1];
                            //}

                            motherVo.AMNIOTIC = dicKey["X046"];    // 羊水量(单位：ml)
                            motherVo.PLACENTALLONG = dicKey["X025"];         // 胎盘长（单位cm）
                            motherVo.PLACENTAWIDTH = dicKey["X026"];         // 胎盘宽（单位cm）
                            motherVo.PLACENTALTHICKNESS = dicKey["X027"];    // 胎盘厚（单位cm）

                            // 会阴情况代码; 会阴情况
                            if (dicKey["X049"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "1";
                                motherVo.ISPERINEUMCUT_2 = "完整";
                            }
                            else if (dicKey["X051"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "6";
                                motherVo.ISPERINEUMCUT_2 = "正中切";
                            }
                            else if (dicKey["X052"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "2";
                                motherVo.ISPERINEUMCUT_2 = "侧切";
                            }

                            // 缝合情况代码; 缝合情况
                            if (dicKey["X054"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "3";
                                motherVo.ISPERINEUMCUT_2 = "Ⅰ度裂伤";
                            }
                            else if (dicKey["X055"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "4";
                                motherVo.ISPERINEUMCUT_2 = "Ⅱ度裂伤";
                            }
                            else if (dicKey["X056"] == "1")
                            {
                                motherVo.ISPERINEUMCUT_1 = "5";
                                motherVo.ISPERINEUMCUT_2 = "Ⅲ度裂伤";
                            }

                            // 缝合情况 缝合针数(单位：针)
                            if (!string.IsNullOrEmpty(dicKey["X057"]))
                            {
                                motherVo.SUTURESITUATION_1 = "2";
                                motherVo.SUTURESITUATION_2 = "外缝";
                                motherVo.SEW = dicKey["X057"];
                            }
                            else if (!string.IsNullOrEmpty(dicKey["X058"]))
                            {
                                motherVo.SUTURESITUATION_1 = "1";
                                motherVo.SUTURESITUATION_2 = "皮内缝合";
                                motherVo.SEW = dicKey["X058"];
                            }

                            motherVo.OPERATIONREASON = dicKey["X073"];// 手术原因
                            motherVo.CHUXUE = dicKey["X060"];        // 阴道分娩产后2h出血量（单位：ml）
                            motherVo.SSZXM = dicKey["X108"];                          // 手术人
                            motherVo.ACCUSR = dicKey["X109"];                       // 接生人
                            motherVo.OPERATEDATE = dr["recorddate"].ToString();        // 录入时间
                            motherVo.ORG_1 = "4419060001";
                            motherVo.ORG_2 = "东莞市茶山医院";

                            ////滞产
                            //if (dicKey["ZHICHANG-Y"] == "1")
                            //{
                            //    motherVo.ZHICHANG = "是";
                            //    motherVo.ZHICHANGcode = "1";
                            //}
                            //else if (dicKey["ZHICHANG-N"] == "1")
                            //{
                            //    motherVo.ZHICHANG = "否";
                            //    motherVo.ZHICHANGcode = "0";
                            //}

                            ////危重抢救
                            //if (dicKey["SALVE-Y"] == "1")
                            //{
                            //    motherVo.SALVE = "是";
                            //    motherVo.SALVEcode = "1";
                            //}
                            //else if (dicKey["SALVE-N"] == "1")
                            //{
                            //    motherVo.SALVE = "否";
                            //    motherVo.SALVEcode = "0";
                            //}

                            ////抢救原因
                            //motherVo.QJREASON = dicKey["QJREASON"];

                            ////促进自然分娩措施
                            //string ZHIRANFENMIAN = dicKey["ZHIRANFENMIAN"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(ZHIRANFENMIAN) || ZHIRANFENMIAN.Contains(" "))
                            //{
                            //    motherVo.ZHIRANFENMIAN = ZHIRANFENMIAN.Split(' ')[1];
                            //    motherVo.ZHIRANFENMIANcode = ZHIRANFENMIAN.Split(' ')[0];
                            //}

                            ////疤痕子宫自然分娩
                            //if (dicKey["BAHENZIFENMIAN-Y"] == "1")
                            //{
                            //    motherVo.BAHENZIFENMIAN = "是";
                            //    motherVo.BAHENZIFENMIANcode = "1";
                            //}
                            //else if (dicKey["BAHENZIFENMIAN-N"] == "1")
                            //{
                            //    motherVo.BAHENZIFENMIAN = "否";
                            //    motherVo.BAHENZIFENMIANcode = "0";
                            //}

                            ////子宫破裂
                            //if (dicKey["ZHIGONGPOLIE-Y"] == "1")
                            //{
                            //    motherVo.ZHIGONGPOLIE = "有";
                            //    motherVo.ZHIGONGPOLIEcode = "1";
                            //}
                            //else if (dicKey["ZHIGONGPOLIE-N"] == "1")
                            //{
                            //    motherVo.ZHIGONGPOLIE = "无";
                            //    motherVo.ZHIGONGPOLIEcode = "0";
                            //}

                            ////子宫破裂 发生院内外
                            //if (dicKey["ZHIGONGPOLIEYOU-N"] == "1")
                            //{
                            //    motherVo.ZHIGONGPOLIEYOU = "院内发生";
                            //    motherVo.ZHIGONGPOLIEYOUcode = "0";
                            //}
                            //else if (dicKey["ZHIGONGPOLIEYOU-W"] == "1")
                            //{
                            //    motherVo.ZHIGONGPOLIEYOU = "非院内发生";
                            //    motherVo.ZHIGONGPOLIEYOUcode = "1";
                            //}

                            ////羊水栓塞
                            //if (dicKey["YANGSHUIQUANSHUAN-W"] == "1")
                            //{
                            //    motherVo.YANGSHUIQUANSHUAN = "无";
                            //    motherVo.YANGSHUIQUANSHUANcode = "0";
                            //}
                            //else if (dicKey["YANGSHUIQUANSHUAN-Y"] == "1")
                            //{
                            //    motherVo.YANGSHUIQUANSHUAN = "有";
                            //    motherVo.YANGSHUIQUANSHUANcode = "1";
                            //}

                            ////羊水栓塞 发生院内外
                            //if (dicKey["YANGSHUIQUANSHUAN-YN"] == "1")
                            //{
                            //    motherVo.YANGSHUIQUANSHUANYOU = "院内发生";
                            //    motherVo.YANGSHUIQUANSHUANYOUcode = "0";
                            //}
                            //else if (dicKey["YANGSHUIQUANSHUAN-YW"] == "1")
                            //{
                            //    motherVo.YANGSHUIQUANSHUANYOU = "非院内发生";
                            //    motherVo.YANGSHUIQUANSHUANYOUcode = "1";
                            //}

                            //手术者职称
                            string SSZZC = dicKey["SSZZC"].TrimStart().TrimEnd();
                            if (!string.IsNullOrEmpty(SSZZC) && SSZZC.Contains(" "))
                            {
                                motherVo.SSZZC = SSZZC.Split(' ')[1];
                                motherVo.SSZZCcode = SSZZC.Split(' ')[0];
                            }

                            //motherVo.BIRTHCERTIFICATENO = dicKey["BIRTHCERTIFICATENO"];//计划生育证明证件号
                            motherVo.INHOSPITAL = dicKey["PatientInDate"];//入院时间
                            motherVo.OUTHOSPITAL = dicKey["PatientOutDate"];//出院时间

                            ////阴道试产转剖宫产
                            //if (dicKey["YINDAOZHANGPAOFU-Y"] == "1")
                            //{
                            //    motherVo.YINDAOZHANGPAOFU = "是";
                            //    motherVo.YINDAOZHANGPAOFUcode = "1";
                            //}
                            //else if (dicKey["YINDAOZHANGPAOFU-N"] == "1")
                            //{
                            //    motherVo.YINDAOZHANGPAOFU = "否";
                            //    motherVo.YINDAOZHANGPAOFUcode = "0";
                            //}

                            //if (dicKey["CHANGHOUQIAN"] == "1")
                            //    motherVo.CHANGHOUQIAN = "高"; //产后血压高
                            //if (dicKey["CHANGHOUHOU"] == "1")
                            //    motherVo.CHANGHOUHOU = "低";// 产后血压低

                            ////新法接生
                            //if (dicKey["NEWAYBIRTH-Y"] == "1")
                            //{
                            //    motherVo.NEWWAYBIRTH = "是";
                            //    motherVo.NEWWAYBIRTHcode = "1";
                            //}
                            //else if (dicKey["NEWAYBIRTH-N"] == "1")
                            //{
                            //    motherVo.NEWWAYBIRTH = "否";
                            //    motherVo.NEWWAYBIRTHcode = "0";
                            //}

                            ////手术产情况
                            //string OPERATION = dicKey["OPERATION"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(OPERATION) && OPERATION.Contains(" "))
                            //{
                            //    motherVo.OPERATION = OPERATION.Split(' ')[1];
                            //    motherVo.OPERATIONcode = OPERATION.Split(' ')[0];
                            //}

                            ////剖宫产指征
                            //string PAUNCH = dicKey["PAUNCH"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(PAUNCH) && PAUNCH.Contains(" "))
                            //{
                            //    motherVo.PAUNCH = PAUNCH.Split(' ')[1];
                            //    motherVo.PAUNCHcode = PAUNCH.Split(' ')[0];
                            //}
                            ////其他剖宫产指征
                            //motherVo.OTHERPAUNCH = dicKey["OTHERPAUNCH"];

                            ////并发症或合并症代码
                            //string TOGETHERILL = dicKey["TOGETHERILL"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(TOGETHERILL) && TOGETHERILL.Contains(" "))
                            //{
                            //    motherVo.TOGETHERILL = TOGETHERILL.Split(' ')[1];
                            //    motherVo.TOGETHERILLcode = TOGETHERILL.Split(' ')[0];
                            //}
                            ////出血原因
                            //string BLEEDCAUSE = dicKey["BLEEDCAUSE"].TrimStart().TrimEnd();
                            //if (!string.IsNullOrEmpty(BLEEDCAUSE) && BLEEDCAUSE.Contains(" "))
                            //{
                            //    motherVo.BLEEDCAUSE = BLEEDCAUSE.Split(' ')[1];
                            //    motherVo.BLEEDCAUSEcode = BLEEDCAUSE.Split(' ')[0];
                            //}
                            ////重度子痫前期
                            //if (dicKey["ZHONGDUZIXIAN-W"] == "1")
                            //{
                            //    motherVo.ZHONGDUZIXIAN = "无";
                            //    motherVo.ZHONGDUZIXIANcode = "0";
                            //}
                            //else if (dicKey["ZHONGDUZIXIAN-Y"] == "1")
                            //{
                            //    motherVo.ZHONGDUZIXIAN = "有";
                            //    motherVo.ZHONGDUZIXIANcode = "1";
                            //}
                            motherVo.LAOJIZHOU = dicKey["X036"];//绕颈几周
                            motherVo.ZHONGLIANG = dicKey["X099"];//重量

                            motherVo.lstChild = new List<EntityChild>();
                            EntityChild childVo = new EntityChild();

                            // 婴儿性别代码; 婴儿性别
                            if (dicKey["X082"] == "1")
                            {
                                childVo.SEX_1 = "1";
                                childVo.SEX_2 = "男";
                            }
                            else
                            {
                                childVo.SEX_1 = "2";
                                childVo.SEX_2 = "女";
                            }

                            childVo.SEQUENCE = dicKey["SEQUENCE"];                   // 胎次
                            childVo.BABYNAME = motherVo.NAME + childVo.SEX_2 + childVo.SEQUENCE;
                            childVo.DATEOFBIRTH = dicKey["X009"];  // 出生时间
                            childVo.AVOIRDUPOIS = dicKey["X099"];          // 体重
                            childVo.STATURE = dicKey["X100"];                      // 身长
                            childVo.TOUWEI = dicKey["X101"];                         // 头围

                            childVo.XIONGWEI = "";//胸围</XIONGWEI>

                            ////新生儿出生情况  1活产 2死胎 3死产 4七天内新生儿死亡 9其他
                            //string HEALTH = dicKey["HEALTH"];
                            //if (!string.IsNullOrEmpty(HEALTH) && HEALTH.Contains("-"))
                            //{
                            //    childVo.HEALTH = HEALTH.Split('-')[1];
                            //    childVo.HEALTHcode = HEALTH.Split('-')[0];
                            //}
                            ////新生儿死亡 0-无1-早期死亡(<=7天死亡)  2-晚期死亡(>7天死亡)
                            //string ISDEAD = dicKey["ISDEAD"];
                            //if (!string.IsNullOrEmpty(ISDEAD) && ISDEAD.Contains("-"))
                            //{
                            //    childVo.ISDEAD = ISDEAD.Split('-')[1];
                            //    childVo.ISDEADcode = ISDEAD.Split('-')[0];
                            //}
                            ////新生儿抢救 1-无 2-吸粘液 3-气管插管4-正压给氧 5-药物 6-其他
                            //string HARDHELP = dicKey["HARDHELP"];
                            //if (!string.IsNullOrEmpty(HARDHELP) && HARDHELP.Contains("-"))
                            //{
                            //    childVo.HARDHELP = HARDHELP.Split('-')[1];
                            //    childVo.HARDHELPcode = HARDHELP.Split('-')[0];
                            //}

                            //新生儿窒息0 否 1是
                            if (dicKey["X091"] == "1")
                            {
                                childVo.NEWHUXIcode = "0";
                                childVo.NEWHUXI = "无";
                            }
                            else if (dicKey["X092"] == "1")
                            {
                                childVo.NEWHUXI = "是";
                                childVo.NEWHUXIcode = "1";
                            }
                            //窒息程度 0重度  1其他
                            if (dicKey["X094"] == "1")
                            {
                                childVo.NEWHUXIYOUcode = "0";
                                childVo.NEWHUXIYOU = "重度";
                            }
                            else
                            {
                                childVo.NEWHUXIYOUcode = "1";
                                childVo.NEWHUXIYOU = "其他";
                            }
                            ////新生儿并发症 0 否 1是
                            //if (dicKey["NEWILL-N"] == "1")
                            //{
                            //    childVo.NEWILLcode = "0";
                            //    childVo.NEWILL = "否";
                            //}
                            //else if (dicKey["NEWILL-Y"] == "1")
                            //{
                            //    childVo.NEWILLcode = "1";
                            //    childVo.NEWILL = "是";
                            //}
                            ////新生儿吸入性肺炎 0 否 1是
                            //if (dicKey["NEWFEIYAN-N"] == "1")
                            //{
                            //    childVo.NEWFEIYANcode = "0";
                            //    childVo.NEWFEIYAN = "否";
                            //}
                            //else if (dicKey["NEWFEIYAN-Y"] == "1")
                            //{
                            //    childVo.NEWFEIYANcode = "1";
                            //    childVo.NEWFEIYAN = "是";
                            //}
                            ////新生儿破伤风 0-未查 1-否 2-是
                            //if (dicKey["ISTETANUS-Y"] == "1")
                            //{
                            //    childVo.ISTETANUScode = "2";
                            //    childVo.ISTETANUS = "是";
                            //}
                            //else if (dicKey["ISTETANUS-N"] == "1")
                            //{
                            //    childVo.ISTETANUScode = "1";
                            //    childVo.ISTETANUS = "否";
                            //}
                            //else
                            //{
                            //    childVo.ISTETANUScode = "0";
                            //    childVo.ISTETANUS = "未查";
                            //}

                            //// 是否畸形代码; 是否畸形
                            //if (dicKey["ISBUG-Y"] == "1")
                            //{
                            //    childVo.ISBUG_1 = "2";
                            //    childVo.ISBUG_2 = "是";
                            //}
                            //else if (dicKey["ISBUG-N"] == "1")
                            //{
                            //    childVo.ISBUG_1 = "1";
                            //    childVo.ISBUG_2 = "否";
                            //}
                            //else
                            //{
                            //    childVo.ISBUG_1 = "0";
                            //    childVo.ISBUG_2 = "未查";
                            //}

                            motherVo.lstChild.Add(childVo);

                            recordStr += string.Format("<HISID>{0}</HISID>", motherVo.HISID) + Environment.NewLine;                          // HIS系统唯一ID
                            recordStr += string.Format("<BARCODE>{0}</BARCODE>", motherVo.BARCODE) + Environment.NewLine;                    // 孕产妇保健手册号
                            recordStr += string.Format("<IDCARD>{0}</IDCARD>", motherVo.IDCARD) + Environment.NewLine;                       // 女方身份证号
                            #region 母亲 父亲 基本信息
                            recordStr += string.Format("<NAME>{0}</NAME>", motherVo.NAME) + Environment.NewLine;                             // 母亲姓名
                            recordStr += string.Format("<HDSB0101021>{0}</HDSB0101021>", motherVo.HDSB0101021) + Environment.NewLine;        // 母亲出生日期
                            recordStr += string.Format("<HDSB0101022 code=\"{0}\" codeSystem=\"GB/T 2659-2000\">{1}</HDSB0101022>", motherVo.HDSB0101022_1, motherVo.HDSB0101022_2) + Environment.NewLine;   // 母亲国籍代码; 母亲国籍
                            recordStr += string.Format("<HDSB0101023 code=\"{0}\" codeSystem=\"GB 3304-1991\">{1}</HDSB0101023>", motherVo.HDSB0101023_1, motherVo.HDSB0101023_2) + Environment.NewLine;     // 母亲民族代码; 母亲民族
                            recordStr += string.Format("<HDSB0101024 code=\"{0}\" codeSystem=\"CV02.01.101\">{1}</HDSB0101024>", motherVo.HDSB0101024_1, motherVo.HDSB0101024_2) + Environment.NewLine;      // 母亲身份证件类别代码; 母亲身份证件类别名
                            recordStr += string.Format("<HDSB0101025>{0}</HDSB0101025>", motherVo.HDSB0101025) + Environment.NewLine;        // 母亲身份证件号码
                            recordStr += string.Format("<HDSB0101040 code=\"{0}\" codeSystem=\"GBT 2260—2012\">{1}</HDSB0101040>", motherVo.HDSB0101040_1, motherVo.HDSB0101040_2) + Environment.NewLine;   // 母亲户籍地址区划代码; 母亲户籍地址
                            recordStr += string.Format("<HDSB0101045>{0}</HDSB0101045>", motherVo.HDSB0101045) + Environment.NewLine;        // 母亲详细户籍地址(包括门牌号)
                            recordStr += string.Format("<PRESENTADDRESS code=\"{0}\" codeSystem=\"GBT 2260—2012\">{1}</PRESENTADDRESS>", motherVo.PRESENTADDRESS_1, motherVo.PRESENTADDRESS_2) + Environment.NewLine;    // 母亲现住地址行政区划代码; 母亲现住地址
                            recordStr += string.Format("<FULLPRESENTADDRESS>{0}</FULLPRESENTADDRESS>", motherVo.FULLPRESENTADDRESS) + Environment.NewLine;                                                   // 母亲详细现住地址(包括门牌号)

                            recordStr += string.Format("<HDSB0101026>{0}</HDSB0101026>", motherVo.HDSB0101026) + Environment.NewLine;        // 父亲姓名
                            recordStr += string.Format("<HDSB0101027>{0}</HDSB0101027>", motherVo.HDSB0101027) + Environment.NewLine;        // 父亲出生日期
                            recordStr += string.Format("<HDSB0101028 code=\"{0}\" codeSystem=\"GB/T 2659-2000\">{1}</HDSB0101028>", motherVo.HDSB0101028_1, motherVo.HDSB0101028_2) + Environment.NewLine;   // 父亲国籍代码; 父亲国籍
                            recordStr += string.Format("<HDSB0101029 code=\"{0}\" codeSystem=\"GB 3304-1991\">{1}</HDSB0101029>", motherVo.HDSB0101029_1, motherVo.HDSB0101029_2) + Environment.NewLine;     // 父亲民族代码; 父亲民族
                            recordStr += string.Format("<HDSB0101030 code=\"{0}\" codeSystem=\"CV02.01.101\">{1}</HDSB0101030>", motherVo.HDSB0101030_1, motherVo.HDSB0101030_2) + Environment.NewLine;      // 父亲身份证件类别代码; 父亲身份证件类别名
                            recordStr += string.Format("<HDSB0101031>{0}</HDSB0101031>", motherVo.HDSB0101031) + Environment.NewLine;        // 父亲身份证件号码
                            recordStr += string.Format("<HDSB0101046 code=\"{0}\" codeSystem=\"GBT 2260—2012\">{1}</HDSB0101046>", motherVo.HDSB0101046_1, motherVo.HDSB0101046_2) + Environment.NewLine;   // 父亲户籍地址区划代码; 父亲户籍地址
                            recordStr += string.Format("<HDSB0101051>{0}</HDSB0101051>", motherVo.HDSB0101051) + Environment.NewLine;        // 父亲详细户籍地址(包括门牌号)
                            recordStr += string.Format("<HPRESENTADDRESS code=\"{0}\" codeSystem=\"GBT 2260—2012\">{1}</HPRESENTADDRESS>", motherVo.HPRESENTADDRESS_1, motherVo.HPRESENTADDRESS_2) + Environment.NewLine;    // 父亲现住地址行政区划代码; 父亲现住地址
                            recordStr += string.Format("<HFULLPRESENTADDRESS>{0}</HFULLPRESENTADDRESS>", motherVo.HFULLPRESENTADDRESS) + Environment.NewLine;                                                // 父亲详细现住地址(包括门牌号)
                            #endregion

                            recordStr += string.Format("<MATTER code=\"{0}\" codesystem=\"STD_ISSUEREASON\">{1}</MATTER>", motherVo.MATTER_1, motherVo.MATTER_2) + Environment.NewLine;                      // 签发原因代码; 签发原因（00：信息齐全(双亲)，02：信息不全(单亲)）
                            recordStr += string.Format("<BEDNO>{0}</BEDNO>", motherVo.BEDNO) + Environment.NewLine;                          // 床号
                            recordStr += string.Format("<ZYH>{0}</ZYH>", motherVo.ZYH) + Environment.NewLine;                                // 住院号
                            recordStr += string.Format("<INTIRE>{0}</INTIRE>", motherVo.INTIRE) + Environment.NewLine;                       // 当前第几胎
                            recordStr += string.Format("<INHOSPITALIZATIONIN>{0}</INHOSPITALIZATIONIN>", motherVo.INHOSPITALIZATIONIN) + Environment.NewLine;                                                // 当前第几次住院
                            recordStr += string.Format("<PLACETYPE code=\"{0}\" codesystem=\"STD_PLACETYPE\">{1}</PLACETYPE>", motherVo.PLACETYPE_1, motherVo.PLACETYPE_2) + Environment.NewLine;            // 分娩地点类型代码; 分娩地点类型名称
                            recordStr += string.Format("<CYESISWEEK>{0}</CYESISWEEK>", motherVo.CYESISWEEK) + Environment.NewLine;           // 分娩孕周(日)
                            recordStr += string.Format("<FETUSNUMBER code=\"{0}\" codesystem=\"STD_FETUSNUM\">{1}</FETUSNUMBER>", motherVo.FETUSNUMBER_1, motherVo.FETUSNUMBER_2) + Environment.NewLine;     // 胎数代码; 胎数
                            recordStr += string.Format("<TAIMOPOLIEFANGSHI code=\"{0}\" codesystem=\"STD_TAIMOPOLIE\">{1}</TAIMOPOLIEFANGSHI>", motherVo.TAIMOPOLIEFANGSHI_1, motherVo.TAIMOPOLIEFANGSHI_2) + Environment.NewLine;   // 胎膜破裂方式代码; 胎膜破裂方式名称
                            recordStr += string.Format("<TAIMOPOLIE>{0}</TAIMOPOLIE>", motherVo.TAIMOPOLIE) + Environment.NewLine;           // 胎膜破裂时间
                            recordStr += string.Format("<CHILDBIRTHTIME>{0}</CHILDBIRTHTIME>", motherVo.CHILDBIRTHTIME) + Environment.NewLine;                                                               // 分娩时间
                            recordStr += string.Format("<CHIBIRTYPE code=\"{0}\" codesystem=\"STD_CHIBIRTYPE\">{1}</CHIBIRTYPE>", motherVo.CHIBIRTYPE_1, motherVo.CHIBIRTYPE_2) + Environment.NewLine;       // 分娩方式代码; 分娩方式
                            recordStr += string.Format("<FETUSPOSITION code=\"{0}\" codesystem=\"STD_FETUSPOSITION\">{1}</FETUSPOSITION>", motherVo.FETUSPOSITION_1, motherVo.FETUSPOSITION_2) + Environment.NewLine;    // 胎方位代码; 胎方位
                            recordStr += string.Format("<ONELAYHOUR>{0}</ONELAYHOUR>", motherVo.ONELAYHOUR) + Environment.NewLine;           // 第一产程（小时）
                            recordStr += string.Format("<ONELAY>{0}</ONELAY>", motherVo.ONELAY) + Environment.NewLine;                       // 第一产程（分钟）
                            recordStr += string.Format("<TWOLAYHOUR>{0}</TWOLAYHOUR>", motherVo.TWOLAYHOUR) + Environment.NewLine;           // 第二产程（小时）
                            recordStr += string.Format("<TWOLAY>{0}</TWOLAY>", motherVo.TWOLAY) + Environment.NewLine;                       // 第二产程（分钟）
                            recordStr += string.Format("<THREELAYHOUR>{0}</THREELAYHOUR>", motherVo.THREELAYHOUR) + Environment.NewLine;     // 第三产程（小时）
                            recordStr += string.Format("<THREELAY>{0}</THREELAY>", motherVo.THREELAY) + Environment.NewLine;                 // 第三产程（分钟）
                            recordStr += string.Format("<ALLLAYHOUR>{0}</ALLLAYHOUR>", motherVo.ALLLAYHOUR) + Environment.NewLine;           // 总产程（小时）
                            recordStr += string.Format("<ALLLAY>{0}</ALLLAY>", motherVo.ALLLAY) + Environment.NewLine;                       // 总产程（分钟）
                            recordStr += string.Format("<PLACENTALTIME>{0}</PLACENTALTIME>", motherVo.PLACENTALTIME) + Environment.NewLine;  // 胎盘娩出时间
                            recordStr += string.Format("<PLACENTALFANGSHI code=\"{0}\"  codesystem=\"STD_PLACENTALFANGSHI\">{1}</PLACENTALFANGSHI>", motherVo.PLACENTALFANGSHI_1, motherVo.PLACENTALFANGSHI_2) + Environment.NewLine;    // 胎盘娩出方式代码; 胎盘娩出方式
                            recordStr += string.Format("<DELIVERYMEASURES>{0}</DELIVERYMEASURES>", motherVo.DELIVERYMEASURES) + Environment.NewLine;                                                         // 分娩措施
                            recordStr += string.Format("<TAIPAN code=\"{0}\" codesystem=\"STD_TAIPAN\">{1}</TAIPAN>", motherVo.TAIPAN_1, motherVo.TAIPAN_2) + Environment.NewLine;                           // 胎膜胎盘完整性代码; 胎盘完整性
                            recordStr += string.Format("<PLACENTA code=\"{0}\" codesystem=\"STD_PLACENTA\">{1}</PLACENTA>", motherVo.PLACENTA_1, motherVo.PLACENTA_2) + Environment.NewLine;                 // 胎膜完整性代码; 胎膜完整性
                            recordStr += string.Format("<JIDAI>{0}</JIDAI>", motherVo.JIDAI) + Environment.NewLine;                          // 脐带长度(单位：cm)
                            recordStr += string.Format("<LUCIDITY code=\"{0}\" codesystem=\"STD_LUCIDITY\">{1}</LUCIDITY>", motherVo.LUCIDITY_1, motherVo.LUCIDITY_2) + Environment.NewLine;                 // 羊水清否代码; 羊水清否
                            recordStr += string.Format("<DEGREE code=\"{0}\" codesystem=\"STD_DEGREE\">{1}</DEGREE>", motherVo.DEGREE_1, motherVo.DEGREE_2) + Environment.NewLine;                           // 羊水分度代码; 羊水分度
                            recordStr += string.Format("<AMNIOTIC>{0}</AMNIOTIC>", motherVo.AMNIOTIC) + Environment.NewLine;                 // 羊水量(单位：ml)
                            recordStr += string.Format("<PLACENTALLONG>{0}</PLACENTALLONG>", motherVo.PLACENTALLONG) + Environment.NewLine;                                                                  // 胎盘长（单位cm）
                            recordStr += string.Format("<PLACENTAWIDTH>{0}</PLACENTAWIDTH>", motherVo.PLACENTAWIDTH) + Environment.NewLine;                                                                  // 胎盘宽（单位cm）
                            recordStr += string.Format("<PLACENTALTHICKNESS>{0}</PLACENTALTHICKNESS>", motherVo.PLACENTALTHICKNESS) + Environment.NewLine;                                                   // 胎盘厚（单位cm）
                            recordStr += string.Format("<ISPERINEUMCUT code=\"{0}\" codesystem=\"STD_ISPERINEUMCUT\">{1}</ISPERINEUMCUT>", motherVo.ISPERINEUMCUT_1, motherVo.ISPERINEUMCUT_2) + Environment.NewLine;    // 会阴情况代码; 会阴情况
                            recordStr += string.Format("<SUTURESITUATION code=\"{0}\" codesystem=\"STD_SUTURESITUATION\">{1}</SUTURESITUATION>", motherVo.SUTURESITUATION_1, motherVo.SUTURESITUATION_2) + Environment.NewLine;  // 缝合情况代码; 缝合情况
                            recordStr += string.Format("<SEW>{0}</SEW>", motherVo.SEW) + Environment.NewLine;                                // 缝合针数(单位：针)
                            recordStr += string.Format("<OPERATIONREASON>{0}</OPERATIONREASON>", motherVo.OPERATIONREASON) + Environment.NewLine;// 手术原因
                            recordStr += string.Format("<CHUXUE>{0}</CHUXUE>", motherVo.CHUXUE) + Environment.NewLine;                       // 阴道分娩产后2h出血量（单位：ml）
                            recordStr += string.Format("<SSZXM>{0}</SSZXM>", motherVo.SSZXM) + Environment.NewLine;                          // 手术人
                            recordStr += string.Format("<ACCUSR>{0}</ACCUSR>", motherVo.ACCUSR) + Environment.NewLine;                       // 接生人
                            recordStr += string.Format("<OPERATEDATE>{0}</OPERATEDATE>", motherVo.OPERATEDATE) + Environment.NewLine;        // 录入时间
                            recordStr += string.Format("<ORG code=\"{0}\" codesystem=\"STD_ORGAN\">{1}</ORG>", motherVo.ORG_1, motherVo.ORG_2) + Environment.NewLine;

                            recordStr += string.Format("<ZHICHANG code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</ZHICHANG>", motherVo.ZHICHANGcode, motherVo.ZHICHANG) + Environment.NewLine;
                            recordStr += string.Format("<SALVE code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</SALVE>", motherVo.SALVEcode, motherVo.SALVE) + Environment.NewLine;
                            recordStr += string.Format("<QJREASON>{0}</QJREASON>", motherVo.QJREASON) + Environment.NewLine;
                            recordStr += string.Format("<ZHIRANFENMIAN code=\"{0}\" codesystem=\"STD_DELIVERYMEASURES\">{1}</ZHIRANFENMIAN>", motherVo.ZHIRANFENMIANcode, motherVo.ZHIRANFENMIAN) + Environment.NewLine;
                            recordStr += string.Format("<BAHENZIFENMIAN code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</BAHENZIFENMIAN>", motherVo.BAHENZIFENMIANcode, motherVo.BAHENZIFENMIAN) + Environment.NewLine;
                            recordStr += string.Format("<ZHIGONGPOLIE code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</ZHIGONGPOLIE>", motherVo.ZHIGONGPOLIEcode, motherVo.ZHIGONGPOLIE) + Environment.NewLine;
                            recordStr += string.Format("<ZHIGONGPOLIEYOU code=\"{0}\" codesystem=\"STD_YUANNEIWAI\">{1}</ZHIGONGPOLIEYOU>", motherVo.ZHIGONGPOLIEYOUcode, motherVo.ZHIGONGPOLIEYOU) + Environment.NewLine;
                            recordStr += string.Format("<YANGSHUIQUANSHUAN code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</YANGSHUIQUANSHUAN>", motherVo.YANGSHUIQUANSHUANcode, motherVo.YANGSHUIQUANSHUAN) + Environment.NewLine;
                            recordStr += string.Format("<YANGSHUIQUANSHUANYOU code=\"{0}\" codesystem=\"STD_YUANNEIWAI\">{1}</YANGSHUIQUANSHUANYOU>", motherVo.YANGSHUIQUANSHUANYOUcode, motherVo.YANGSHUIQUANSHUANYOU) + Environment.NewLine;
                            recordStr += string.Format("<SSZZC code=\"{0}\" codesystem=\"STD_SSZZC\">{1}</SSZZC>", motherVo.SSZZCcode, motherVo.SSZZC) + Environment.NewLine;
                            recordStr += string.Format("<BIRTHCERTIFICATENO>{0}</BIRTHCERTIFICATENO>", motherVo.BIRTHCERTIFICATENO) + Environment.NewLine;
                            recordStr += string.Format("<INHOSPITAL>{0}</INHOSPITAL>", motherVo.INHOSPITAL) + Environment.NewLine;
                            recordStr += string.Format("<OUTHOSPITAL>{0}</OUTHOSPITAL>", motherVo.OUTHOSPITAL) + Environment.NewLine;
                            recordStr += string.Format("<YINDAOZHANGPAOFU code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</YINDAOZHANGPAOFU>", motherVo.YINDAOZHANGPAOFUcode, motherVo.YINDAOZHANGPAOFU) + Environment.NewLine;
                            recordStr += string.Format("<CHANGHOUQIAN>{0}</CHANGHOUQIAN>", motherVo.CHANGHOUQIAN) + Environment.NewLine;
                            recordStr += string.Format("<CHANGHOUHOU>{0}</CHANGHOUHOU>", motherVo.CHANGHOUHOU) + Environment.NewLine;
                            recordStr += string.Format("<NEWWAYBIRTH code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</NEWWAYBIRTH>", motherVo.NEWWAYBIRTHcode, motherVo.NEWWAYBIRTH) + Environment.NewLine;
                            recordStr += string.Format("<ACCORG code=\"{0}\" codesystem=\"STD_ORGAN\">{1}</ACCORG>", motherVo.ACCORGcode, motherVo.ACCORG) + Environment.NewLine;
                            recordStr += string.Format("<OPERATION code=\"{0}\" codesystem=\"STD_OPERATION\">{1}</OPERATION>", motherVo.OPERATIONcode, motherVo.OPERATION) + Environment.NewLine;
                            recordStr += string.Format("<PAUNCH code=\"{0}\" codesystem=\"STD_PAUNCH\">{1}</PAUNCH>", motherVo.PAUNCHcode, motherVo.PAUNCH) + Environment.NewLine;
                            recordStr += string.Format("<OTHERPAUNCH>{0}</OTHERPAUNCH>", motherVo.OTHERPAUNCH) + Environment.NewLine;
                            recordStr += string.Format("<TOGETHERILL code=\"{0}\" codesystem=\"STD_TOGETHERILL\">{1}</TOGETHERILL>", motherVo.TOGETHERILLcode, motherVo.TOGETHERILL) + Environment.NewLine;
                            recordStr += string.Format("<TOGETHERILLOTHER>{0}</TOGETHERILLOTHER>", motherVo.TOGETHERILLOTHER) + Environment.NewLine;
                            recordStr += string.Format("<BLEEDCAUSE code=\"{0}\" codesystem=\"STD_BLEEDCAUSE\">{1}</BLEEDCAUSE>", motherVo.BLEEDCAUSEcode, motherVo.BLEEDCAUSE) + Environment.NewLine;
                            recordStr += string.Format("<LAOJIZHOU>{0}</LAOJIZHOU>", motherVo.LAOJIZHOU) + Environment.NewLine;
                            recordStr += string.Format("<ZHONGLIANG>{0}</ZHONGLIANG>", motherVo.ZHONGLIANG) + Environment.NewLine;
                            recordStr += string.Format("<ZHONGDUZIXIAN code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</ZHONGDUZIXIAN>", motherVo.ZHONGDUZIXIANcode, motherVo.ZHONGDUZIXIAN) + Environment.NewLine;
                            recordStr += string.Format("<RELAXADDRCODE code=\"{0}\" codesystem=\"GBT 2260—2012\">{1}</RELAXADDRCODE>", "0", "无") + Environment.NewLine;
                            recordStr += string.Format("<RELAXADDR></RELAXADDR>") + Environment.NewLine;
                            recordStr += string.Format("<VISITORG code=\"{0}\" codesystem=\"STD_ORGAN\">{1}</VISITORG>", "0", "无") + Environment.NewLine;
                            recordStr += string.Format("<POSTTEL></POSTTEL>") + Environment.NewLine;

                            // 录入单位机构代码; 录入单位
                            foreach (EntityChild vo in motherVo.lstChild)
                            {
                                recordStr += "<BABY>" + Environment.NewLine;
                                recordStr += string.Format("<BABYNAME>{0}</BABYNAME>", vo.BABYNAME) + Environment.NewLine;                   // 婴儿姓名
                                recordStr += string.Format("<SEX code=\"{0}\" codesystem=\"GB/T 2261.1\">{1}</SEX>", vo.SEX_1, vo.SEX_2) + Environment.NewLine;                                              // 婴儿性别代码; 婴儿性别
                                recordStr += string.Format("<SEQUENCE>{0}</SEQUENCE>", vo.SEQUENCE) + Environment.NewLine;                   // 胎次
                                recordStr += string.Format("<DATEOFBIRTH>{0}</DATEOFBIRTH>", vo.DATEOFBIRTH) + Environment.NewLine;          // 出生时间
                                recordStr += string.Format("<AVOIRDUPOIS>{0}</AVOIRDUPOIS>", vo.AVOIRDUPOIS) + Environment.NewLine;          // 体重
                                recordStr += string.Format("<STATURE>{0}</STATURE>", vo.STATURE) + Environment.NewLine;                      // 身长
                                recordStr += string.Format("<TOUWEI>{0}</TOUWEI>", vo.TOUWEI) + Environment.NewLine;                         // 头围
                                recordStr += string.Format("<XIONGWEI>{0}</XIONGWEI>", vo.XIONGWEI) + Environment.NewLine;
                                recordStr += string.Format("<HEALTH code=\"{0}\" codesystem=\"STD_HEALTH\">{1}</HEALTH>", vo.HEALTHcode, vo.HEALTH) + Environment.NewLine;
                                recordStr += string.Format("<ISDEAD code=\"{0}\" codesystem=\"STD_ISDEAD\">{1}</ISDEAD>", vo.ISDEADcode, vo.ISDEAD) + Environment.NewLine;
                                recordStr += string.Format("<HARDHELP code=\"{0}\" codesystem=\"STD_HARDHELP\">{1}</HARDHELP>", vo.HARDHELPcode, vo.HARDHELP) + Environment.NewLine;
                                recordStr += string.Format("<NEWHUXI code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</NEWHUXI>", vo.NEWHUXIcode, vo.NEWHUXI) + Environment.NewLine;
                                recordStr += string.Format("<NEWHUXIYOU code=\"{0}\" codesystem=\"STD_NEWHUXIYOU\">{1}</NEWHUXIYOU>", vo.NEWHUXIYOUcode, vo.NEWHUXIYOU) + Environment.NewLine;
                                recordStr += string.Format("<NEWILL code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</NEWILL>", vo.NEWILLcode, vo.NEWILL) + Environment.NewLine;
                                recordStr += string.Format("<NEWFEIYAN code=\"{0}\" codesystem=\"STD_YESORNO\">{1}</NEWFEIYAN>", vo.NEWFEIYANcode, vo.NEWFEIYAN) + Environment.NewLine;
                                recordStr += string.Format("<ISBUG code=\"{0}\" codesystem=\"STD_ISBUG\">{1}</ISBUG>", vo.ISBUG_1, vo.ISBUG_2) + Environment.NewLine;                                        // 是否畸形代码; 是否畸形
                                recordStr += string.Format("<ISTETANUS code=\"{0}\" codesystem=\"STD_ISNNT\">{1}</ISTETANUS>", vo.ISTETANUScode, vo.ISTETANUS) + Environment.NewLine;
                                recordStr += string.Format("<APGAR1>{0}</APGAR1>", vo.APGAR1) + Environment.NewLine;                         // 1min Apgar总分
                                recordStr += string.Format("<APGAR5>{0}</APGAR5>", vo.APGAR5) + Environment.NewLine;                         // 5min Apgar总分
                                recordStr += string.Format("<APGAR10>{0}</APGAR10>", vo.APGAR10) + Environment.NewLine;                      // 10min Apgar总分
                                recordStr += string.Format("<HBIGTIME code=\"{0}\" codesystem=\"STD_HBIGTIME\">{1}</HBIGTIME>", vo.HBIGTIME_1, vo.HBIGTIME_2) + Environment.NewLine;                         // 是否注射乙肝免疫球蛋白代码; 是否注射乙肝免疫球蛋白
                                recordStr += string.Format("<INJECTIONDATE>{0}</INJECTIONDATE>", vo.INJECTIONDATE) + Environment.NewLine;                                                                    // 注射日期
                                recordStr += string.Format("<JILIANG>{0}</JILIANG>", vo.JILIANG) + Environment.NewLine;                                                                                      // 注射剂量（单位：IU）
                                recordStr += string.Format("<SKINCONTACT code=\"{0}\" codesystem=\"STD_SKINCONTACT\">{1}</SKINCONTACT>", vo.SKINCONTACT_1, vo.SKINCONTACT_2) + Environment.NewLine;          // 产后30分钟内皮肤接触情况代码; 产后30分钟内皮肤接触情况
                                recordStr += "</BABY>" + Environment.NewLine;
                            }
                            recordStr += "</record>" + Environment.NewLine;

                            xmlUpload = string.Format(xmlUpload, recordStr);
                            Log.Output("上传信息：" + Environment.NewLine + xmlUpload);

                            WebService ws = new WebService();

                            string res = ws.SaveInfoStringTypeXML("A74CC68F-B009-4264-A880-FBE87DD91E56", "763709818", xmlUpload);

                            Log.Output("返回信息：" + Environment.NewLine + res); //Encoding.Default.GetString(res));

                            #region 保存上传记录

                            doc.LoadXml(res);
                            string affect = doc["Document"]["component"]["OperationSuccess"].Attributes["value"].Value;
                            if (affect != "YES")
                            {
                                continue;
                            }

                            List<DacParm> lstParm = new List<DacParm>();

                            registerId += motherVo.RegisterId + ",";
                            uploadCount++;

                            try
                            {
                                string womanid = doc["Document"]["component"]["OperationSuccess"]["recordID"].Attributes["womanid"].Value;
                                Sql = @"delete from t_opr_bih_wacrecord where registerid = ?  ";
                                string Sql1 = @"insert into t_opr_bih_wacrecord values (?, ?, ?, ? , ?)";

                                parm = svc.CreateParm(1);
                                parm[0].Value = motherVo.RegisterId;
                                lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql, parm));

                                parm = svc.CreateParm(5);
                                parm[0].Value = motherVo.RegisterId;
                                parm[1].Value = motherVo.ipNo;
                                parm[2].Value = Function.Datetime(motherVo.inpatientDate) ; 
                                parm[3].Value = DateTime.Now;
                                parm[4].Value = womanid;
                                lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql1, parm));

                                if (lstParm.Count > 0)
                                {
                                    svc.Commit(lstParm);
                                }
                            }
                            catch (Exception e)
                            {
                                ExceptionLog.OutPutException(e);
                            }

                            #endregion
                        }
                        #endregion
                    }
                }

                if (!string.IsNullOrEmpty(registerId))
                {
                    registerId = registerId.TrimEnd(',');
                    EntitySysTaskLog logVo = new EntitySysTaskLog();
                    logVo.typeId = "0006";
                    logVo.execTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    logVo.ipAddr = Function.LocalIP();
                    logVo.execStatus = 1;
                    logVo.execDesc = "上传成功 共 " + uploadCount + " 人 " + registerId.TrimEnd(',');
                    this.SaveTaskLog(logVo);
                }
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                //this.progressBarControl.Visible = false;
            }

        }
        #endregion

        #region  咨询
        public string getConsult(EntityMother motherVo)
        {
            string consultStr = string.Empty;
            IDataParameter[] parm = null;
            SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);

            string Sql = @"select 1 from t_def_wacitemrecord a 
                                            where a.uploaddate = to_date(?, 'yyyy-mm-dd hh24:mi:ss') 
                                            and a.patientid = ?
                                            and a.platgroupid in('24','28','35','39') ";
            parm = svc.CreateParm(2);
            parm[0].Value = motherVo.HDSB0101035 + " 00:00:00";
            parm[1].Value = motherVo.HISID;
            DataTable dt = svc.GetDataTable(Sql, parm);
            if (dt != null && dt.Rows.Count > 0)
            {
                return null;
            }
            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 24, "孕期初次接受艾滋病检测相关告知或咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID+"24") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>孕期初次接受艾滋病检测相关告知或咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"54\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>孕期初次接受艾滋病检测相关告知或咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"已告知\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 24, "接受艾滋病咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "24") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受艾滋病咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"88\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>HIV抗体检测前咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 28, "接受艾滋病咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "28") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受艾滋病咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"89\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>HIV抗体检测后咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 35, "接受乙肝咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "35") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受乙肝咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"121\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>乙肝检测后检测</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 35, "接受乙肝咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "35") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受乙肝咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"130\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>乙肝检测前咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>";
            consultStr += "</ASSISTANT>";

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 39, "接受梅毒咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "39") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受梅毒咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"127\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>梅毒检测前咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            consultStr += string.Format("<ASSISTANT code=\"{0}\" codesystem=\"{1}\">", 39, "接受梅毒咨询") + Environment.NewLine;
            consultStr += string.Format("<APPID>{0}</APPID>", motherVo.HISID + "39") + Environment.NewLine;
            consultStr += "<ASSISTANTNAME>接受梅毒咨询</ASSISTANTNAME>" + Environment.NewLine; ;
            consultStr += "<CHKORG code=\"4419060001\" codesystem=\"STD_ORGAN\">东莞市茶山医院</CHKORG>" + Environment.NewLine; ;
            consultStr += "<CHKDEP code=\"25043\" codesystem=\"STD_KESHI\">妇产科</CHKDEP>" + Environment.NewLine; ;
            consultStr += string.Format("<CHKDATE>{0}</CHKDATE>", motherVo.HDSB0101035) + Environment.NewLine; ;
            consultStr += string.Format("<CHKDOCTOR>{0}</CHKDOCTOR>", motherVo.HDSB0101034) + Environment.NewLine; ;
            consultStr += "<RESULT code=\"128\" codesystem=\"STD_RESULT\">" + Environment.NewLine; ;
            consultStr += "<RESULTNAME>梅毒检测后咨询</RESULTNAME>" + Environment.NewLine; ;
            consultStr += "<RESULTVALUE>\"是\"</RESULTVALUE>" + Environment.NewLine; ;
            consultStr += "</RESULT>" + Environment.NewLine; ;
            consultStr += "</ASSISTANT>" + Environment.NewLine; ;

            return consultStr;

        }
        #endregion

        #region insertConsult
        public int insertConsult(EntityMother motherVo)
        {
            int ret = -1;
            List<EntityWacCheckRecord> lstWacCheck = new List<EntityWacCheckRecord>();
            List<DacParm> lstParm = new List<DacParm>();
            IDataParameter[] parm = null;
            SqlHelper svc = new SqlHelper(EnumBiz.onlineDB);

            string Sql = @"select 1 from t_def_wacitemrecord a 
                                            where a.uploaddate = to_date(?, 'yyyy-mm-dd hh24:mi:ss') 
                                            and a.patientid = ?
                                            and a.platgroupid in('24','28','35','39') ";
            parm = svc.CreateParm(2);
            parm[0].Value = motherVo.HDSB0101035 + " 00:00:00";
            parm[1].Value = motherVo.HISID;
            DataTable dt = svc.GetDataTable(Sql, parm);
            if (dt != null && dt.Rows.Count > 0)
            {
                return 0;
            }
            else
            {
                try
                {
                    EntityWacCheckRecord vo1 = new EntityWacCheckRecord();
                    vo1.patientId = motherVo.HISID;
                    vo1.applicationId = motherVo.HISID + "42";
                    vo1.platgroupid = "24";
                    vo1.platgroupname = "孕期初次接受艾滋病检测相关告知或咨询";
                    vo1.hisgroupid = "24";
                    vo1.hisgroupname = "孕期初次接受艾滋病检测相关告知或咨询";
                    vo1.platitemid = "54";
                    vo1.platitemname = "孕期初次接受艾滋病检测相关告知或咨询";
                    vo1.hisitemid = "54";
                    vo1.hisitemname = "孕期初次接受艾滋病检测相关告知或咨询";
                    vo1.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo1.result = "已告知";
                    lstWacCheck.Add(vo1);
                    EntityWacCheckRecord vo2 = new EntityWacCheckRecord();
                    vo2.patientId = motherVo.HISID;
                    vo2.applicationId = motherVo.HISID + "28";
                    vo2.platgroupid = "28";
                    vo2.platgroupname = "接受艾滋病咨询";
                    vo2.hisgroupid = "28";
                    vo2.hisgroupname = "接受艾滋病咨询";
                    vo2.platitemid = "88";
                    vo2.platitemname = "HIV抗体检测前咨询";
                    vo2.hisitemid = "88";
                    vo2.hisitemname = "HIV抗体检测前咨询";
                    vo2.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo2.result = "是";
                    lstWacCheck.Add(vo2);
                    EntityWacCheckRecord vo3 = new EntityWacCheckRecord();
                    vo3.patientId = motherVo.HISID;
                    vo3.applicationId = motherVo.HISID + "28";
                    vo3.platgroupid = "28";
                    vo3.platgroupname = "接受艾滋病咨询";
                    vo3.hisgroupid = "28";
                    vo3.hisgroupname = "接受艾滋病咨询";
                    vo3.platitemid = "89";
                    vo3.platitemname = "HIV抗体检测后咨询";
                    vo3.hisitemid = "89";
                    vo3.hisitemname = "HIV抗体检测后咨询";
                    vo3.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo3.result = "是";
                    lstWacCheck.Add(vo3);
                    EntityWacCheckRecord vo4 = new EntityWacCheckRecord();
                    vo4.patientId = motherVo.HISID;
                    vo4.applicationId = motherVo.HISID + "35";
                    vo4.platgroupid = "35";
                    vo4.platgroupname = "接受乙肝咨询";
                    vo4.hisgroupid = "35";
                    vo4.hisgroupname = "接受乙肝咨询";
                    vo4.platitemid = "121";
                    vo4.platitemname = "乙肝检测后检测";
                    vo4.hisitemid = "121";
                    vo4.hisitemname = "乙肝检测后检测";
                    vo4.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo4.result = "是";
                    lstWacCheck.Add(vo4);
                    EntityWacCheckRecord vo5 = new EntityWacCheckRecord();
                    vo5.patientId = motherVo.HISID;
                    vo5.applicationId = motherVo.HISID + "35";
                    vo5.platgroupid = "35";
                    vo5.platgroupname = "接受乙肝咨询";
                    vo5.hisgroupid = "35";
                    vo5.hisgroupname = "接受乙肝咨询";
                    vo5.platitemid = "130";
                    vo5.platitemname = "乙肝检测前咨询";
                    vo5.hisitemid = "130";
                    vo5.hisitemname = "乙肝检测前咨询";
                    vo5.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo5.result = "是";
                    lstWacCheck.Add(vo5);
                    EntityWacCheckRecord vo6 = new EntityWacCheckRecord();
                    vo6.patientId = motherVo.HISID;
                    vo6.applicationId = motherVo.HISID + "39";
                    vo6.platgroupid = "39";
                    vo6.platgroupname = "接受梅毒咨询";
                    vo6.hisgroupid = "39";
                    vo6.hisgroupname = "接受梅毒咨询";
                    vo6.platitemid = "127";
                    vo6.platitemname = "梅毒检测前咨询";
                    vo6.hisitemid = "127";
                    vo6.hisitemname = "梅毒检测前咨询";
                    vo6.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo6.result = "是";
                    lstWacCheck.Add(vo6);
                    EntityWacCheckRecord vo7 = new EntityWacCheckRecord();
                    vo7.patientId = motherVo.HISID;
                    vo7.applicationId = motherVo.HISID + "39";
                    vo7.platgroupid = "39";
                    vo7.platgroupname = "接受梅毒咨询";
                    vo7.hisgroupid = "39";
                    vo7.hisgroupname = "接受梅毒咨询";
                    vo7.platitemid = "128";
                    vo7.platitemname = "梅毒检测后咨询";
                    vo7.hisitemid = "128";
                    vo7.hisitemname = "梅毒检测后咨询";
                    vo7.uploaddate = Function.Datetime(motherVo.HDSB0101035 + " 00:00:00");
                    vo7.result = "是";
                    lstWacCheck.Add(vo7);

                    Sql = @"insert into t_def_wacitemrecord values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    foreach (EntityWacCheckRecord vo in lstWacCheck)
                    {
                        parm = svc.CreateParm(12);
                        parm[0].Value = vo.patientId;
                        parm[1].Value = vo.applicationId;
                        parm[2].Value = vo.platgroupid;
                        parm[3].Value = vo.platgroupname;
                        parm[4].Value = vo.hisgroupid;
                        parm[5].Value = vo.hisgroupname;
                        parm[6].Value = vo.platitemid;
                        parm[7].Value = vo.platitemname;
                        parm[8].Value = vo.hisitemid;
                        parm[9].Value = vo.hisitemname;
                        parm[10].Value = vo.uploaddate;
                        parm[11].Value = vo.result;

                        lstParm.Add(svc.GetDacParm(EnumExecType.ExecSql, Sql, parm));
                    }

                    if (lstParm.Count > 0)
                    {
                        ret = svc.Commit(lstParm);
                    }
                }
                catch (Exception e)
                {
                    ExceptionLog.OutPutException(e);
                }
            }

            return ret;
        }
        #endregion

        #region 读取XML片段
        /// <summary>
        /// 读取XML片段
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public Dictionary<string, string> ReadXmlNodes(string nodeName, string xml)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            XmlElement element = document[nodeName];
            document = null;

            if (element == null) return null;
            Dictionary<string, string> dicVal = new Dictionary<string, string>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!dicVal.ContainsKey(node.Name))
                {
                    dicVal.Add(node.Name, node.InnerText);
                }
            }
            return dicVal;
        }
        #endregion

        #endregion

        #region 事件

        private void frmConsole_Load(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                this.Init();
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void frmConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == System.Windows.Forms.CloseReason.None)
            {
                e.Cancel = true;
            }
            else
            {
                if (MessageBox.Show("确定退出任务？？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {

                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void frmConsole_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮 
            if (this.WindowState == FormWindowState.Minimized)
            {
                // 隐藏任务栏区图标 
                // this.ShowInTaskbar = false;
                this.Visible = false;
                // 图标显示在托盘区 
                this.notifyIcon.Visible = true;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //判断是否已经最小化于托盘 
            if (WindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                //还原窗体显示 
                WindowState = FormWindowState.Normal;
                //激活窗体并给予它焦点 
                this.Activate();
                //任务栏区显示图标 
                //this.ShowInTaskbar = true;
                //托盘区图标隐藏 
                this.notifyIcon.Visible = false;
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                //UploadFMJL(this.upDate.Text);
                UploadAssistant(this.upDate.Text,this.txtCard.Text);
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                this.RefreshTask(timePoint);
            }
        }

        bool isExecing = false;
        private void timer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.ToString("HH:mm:ss") == timePoint.Trim())
            {
                try
                {
                    if (isExecing) return;
                    isExecing = true;
                    //this.UploadFMJL("");
                    this.UploadAssistant("","");
                }
                finally
                {
                    isExecing = false;
                    this.RefreshTask(timePoint);
                    this.gvTask.ViewCaption = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint;
                }
            }

            if (DateTime.Now.ToString("HH:mm:ss") == timePoint2.Trim())
            {
                try
                {
                    if (isExecing) return;
                    isExecing = true;
                    //this.UploadFMJL("");
                    this.UploadAssistant("", "");
                }
                finally
                {
                    isExecing = false;
                    this.RefreshTask(timePoint2);
                    this.gvTask.ViewCaption = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + timePoint2;
                }
            }
        }

        #endregion

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            try
            {
                //UploadFMJL(this.upDate.Text);
                UploadAssistant2();
            }
            catch (Exception ex)
            {
                Log.Output("异常信息：" + Environment.NewLine + ex.Message);
            }
            finally
            {
                this.RefreshTask(timePoint);
            }
        }

        private void simpleButton1_Click_1(object sender, EventArgs e)
        {
           // UploadAssistant2();
        }
    }

    #region 实体

    #region Mother
    /// <summary>
    /// Mother
    /// </summary>
    public class EntityMother
    {
        /// <summary>
        /// 本次住院唯一ID
        /// </summary>
        public string RegisterId { get; set; }
        /// <summary>
        /// 住院号
        /// </summary>
        public string ipNo { get; set; }
        /// <summary>
        /// 入院时间
        /// </summary>
        public string inpatientDate { get; set; }
        /// <summary>
        /// HIS系统唯一ID
        /// </summary>
        public string HISID { get; set; }
        /// <summary>
        /// 孕产妇保健手册号
        /// </summary>
        public string BARCODE { get; set; }
        /// <summary>
        /// 女方身份证号
        /// </summary>
        public string IDCARD { get; set; }
        /// <summary>
        /// 母亲姓名
        /// </summary>
        public string NAME { get; set; }
        /// <summary>
        /// 母亲出生日期
        /// </summary>
        public string HDSB0101021 { get; set; }
        /// <summary>
        /// 母亲国籍代码
        /// </summary>
        public string HDSB0101022_1 { get; set; }
        /// <summary>
        /// 母亲国籍
        /// </summary>
        public string HDSB0101022_2 { get; set; }
        /// <summary>
        /// 母亲民族代码
        /// </summary>
        public string HDSB0101023_1 { get; set; }
        /// <summary>
        /// 母亲民族
        /// </summary>
        public string HDSB0101023_2 { get; set; }
        /// <summary>
        /// 母亲身份证件类别代码
        /// </summary>
        public string HDSB0101024_1 { get; set; }
        /// <summary>
        /// 母亲身份证件类别名
        /// </summary>
        public string HDSB0101024_2 { get; set; }
        /// <summary>
        /// 母亲身份证件号码
        /// </summary>
        public string HDSB0101025 { get; set; }
        /// <summary>
        /// 母亲户籍地址区划代码
        /// </summary>
        public string HDSB0101040_1 { get; set; }
        /// <summary>
        /// 母亲户籍地址
        /// </summary>
        public string HDSB0101040_2 { get; set; }
        /// <summary>
        /// 母亲详细户籍地址(包括门牌号)
        /// </summary>
        public string HDSB0101045 { get; set; }
        /// <summary>
        /// 母亲现住地址行政区划代码
        /// </summary>
        public string PRESENTADDRESS_1 { get; set; }
        /// <summary>
        /// 母亲现住地址
        /// </summary>
        public string PRESENTADDRESS_2 { get; set; }
        /// <summary>
        /// 母亲详细现住地址(包括门牌号)
        /// </summary>
        public string FULLPRESENTADDRESS { get; set; }
        /// <summary>
        /// 父亲姓名
        /// </summary>
        public string HDSB0101026 { get; set; }
        /// <summary>
        /// 父亲出生日期
        /// </summary>
        public string HDSB0101027 { get; set; }
        /// <summary>
        /// 父亲国籍代码
        /// </summary>
        public string HDSB0101028_1 { get; set; }
        /// <summary>
        /// 父亲国籍
        /// </summary>
        public string HDSB0101028_2 { get; set; }
        /// <summary>
        /// 父亲民族代码
        /// </summary>
        public string HDSB0101029_1 { get; set; }
        /// <summary>
        /// 父亲民族
        /// </summary>
        public string HDSB0101029_2 { get; set; }
        /// <summary>
        /// 父亲身份证件类别代码
        /// </summary>
        public string HDSB0101030_1 { get; set; }
        /// <summary>
        /// 父亲身份证件类别名
        /// </summary>
        public string HDSB0101030_2 { get; set; }
        /// <summary>
        /// 父亲身份证件号码
        /// </summary>
        public string HDSB0101031 { get; set; }
        /// <summary>
        /// 父亲户籍地址区划代码
        /// </summary>
        public string HDSB0101046_1 { get; set; }
        /// <summary>
        /// 父亲户籍地址
        /// </summary>
        public string HDSB0101046_2 { get; set; }
        /// <summary>
        /// 父亲详细户籍地址(包括门牌号)
        /// </summary>
        public string HDSB0101051 { get; set; }
        /// <summary>
        /// 父亲现住地址行政区划代码
        /// </summary>
        public string HPRESENTADDRESS_1 { get; set; }
        /// <summary>
        /// 父亲现住地址
        /// </summary>
        public string HPRESENTADDRESS_2 { get; set; }
        /// <summary>
        /// 父亲详细现住地址(包括门牌号)
        /// </summary>
        public string HFULLPRESENTADDRESS { get; set; }
        /// <summary>
        /// 签发原因代码
        /// </summary>
        public string MATTER_1 { get; set; }
        /// <summary>
        /// 签发原因（00：信息齐全(双亲)，02：信息不全(单亲)）
        /// </summary>
        public string MATTER_2 { get; set; }
        /// <summary>
        /// 床号
        /// </summary>
        public string BEDNO { get; set; }
        /// <summary>
        /// 住院号
        /// </summary>
        public string ZYH { get; set; }
        /// <summary>
        /// 当前第几胎
        /// </summary>
        public string INTIRE { get; set; }
        /// <summary>
        /// 当前第几次住院
        /// </summary>
        public string INHOSPITALIZATIONIN { get; set; }
        /// <summary>
        /// 分娩地点类型代码
        /// </summary>
        public string PLACETYPE_1 { get; set; }
        /// <summary>
        /// 分娩地点类型名称
        /// </summary>
        public string PLACETYPE_2 { get; set; }
        /// <summary>
        /// 分娩孕周(日)
        /// </summary>
        public string CYESISWEEK { get; set; }
        /// <summary>
        /// 胎数代码
        /// </summary>
        public string FETUSNUMBER_1 { get; set; }
        /// <summary>
        /// 胎数
        /// </summary>
        public string FETUSNUMBER_2 { get; set; }
        /// <summary>
        /// 胎膜破裂方式代码
        /// </summary>
        public string TAIMOPOLIEFANGSHI_1 { get; set; }
        /// <summary>
        /// 胎膜破裂方式名称
        /// </summary>
        public string TAIMOPOLIEFANGSHI_2 { get; set; }
        /// <summary>
        /// 胎膜破裂时间
        /// </summary>
        public string TAIMOPOLIE { get; set; }
        /// <summary>
        /// 分娩时间
        /// </summary>
        public string CHILDBIRTHTIME { get; set; }
        /// <summary>
        /// 分娩方式代码
        /// </summary>
        public string CHIBIRTYPE_1 { get; set; }
        /// <summary>
        /// 分娩方式
        /// </summary>
        public string CHIBIRTYPE_2 { get; set; }
        /// <summary>
        /// 胎方位代码
        /// </summary>
        public string FETUSPOSITION_1 { get; set; }
        /// <summary>
        /// 胎方位
        /// </summary>
        public string FETUSPOSITION_2 { get; set; }
        /// <summary>
        /// 第一产程（小时）
        /// </summary>
        public string ONELAYHOUR { get; set; }
        /// <summary>
        /// 第一产程（分钟）
        /// </summary>
        public string ONELAY { get; set; }
        /// <summary>
        /// 第二产程（小时）
        /// </summary>
        public string TWOLAYHOUR { get; set; }
        /// <summary>
        /// 第二产程（分钟）
        /// </summary>
        public string TWOLAY { get; set; }
        /// <summary>
        /// 第三产程（小时）
        /// </summary>
        public string THREELAYHOUR { get; set; }
        /// <summary>
        /// 第三产程（分钟）
        /// </summary>
        public string THREELAY { get; set; }
        /// <summary>
        /// 总产程（小时）
        /// </summary>
        public string ALLLAYHOUR { get; set; }
        /// <summary>
        /// 总产程（分钟）
        /// </summary>
        public string ALLLAY { get; set; }
        /// <summary>
        /// 胎盘娩出时间
        /// </summary>
        public string PLACENTALTIME { get; set; }
        /// <summary>
        /// 胎盘娩出方式代码
        /// </summary>
        public string PLACENTALFANGSHI_1 { get; set; }
        /// <summary>
        /// 胎盘娩出方式
        /// </summary>
        public string PLACENTALFANGSHI_2 { get; set; }
        /// <summary>
        /// 分娩措施
        /// </summary>
        public string DELIVERYMEASURES { get; set; }
        /// <summary>
        /// 胎膜胎盘完整性代码
        /// </summary>
        public string TAIPAN_1 { get; set; }
        /// <summary>
        /// 胎盘完整性
        /// </summary>
        public string TAIPAN_2 { get; set; }
        /// <summary>
        /// 胎膜完整性代码
        /// </summary>
        public string PLACENTA_1 { get; set; }
        /// <summary>
        /// 胎膜完整性
        /// </summary>
        public string PLACENTA_2 { get; set; }
        /// <summary>
        /// 脐带长度(单位：cm)
        /// </summary>
        public string JIDAI { get; set; }
        /// <summary>
        /// 羊水清否代码
        /// </summary>
        public string LUCIDITY_1 { get; set; }
        /// <summary>
        /// 羊水清否
        /// </summary>
        public string LUCIDITY_2 { get; set; }
        /// <summary>
        /// 羊水分度代码
        /// </summary>
        public string DEGREE_1 { get; set; }
        /// <summary>
        /// 羊水分度
        /// </summary>
        public string DEGREE_2 { get; set; }
        /// <summary>
        /// 羊水量(单位：ml)
        /// </summary>
        public string AMNIOTIC { get; set; }
        /// <summary>
        /// 胎盘长（单位cm）
        /// </summary>
        public string PLACENTALLONG { get; set; }
        /// <summary>
        /// 胎盘宽（单位cm）
        /// </summary>
        public string PLACENTAWIDTH { get; set; }
        /// <summary>
        /// 胎盘厚（单位cm）
        /// </summary>
        public string PLACENTALTHICKNESS { get; set; }
        /// <summary>
        /// 会阴情况代码
        /// </summary>
        public string ISPERINEUMCUT_1 { get; set; }
        /// <summary>
        /// 会阴情况
        /// </summary>
        public string ISPERINEUMCUT_2 { get; set; }
        /// <summary>
        /// 缝合情况代码
        /// </summary>
        public string SUTURESITUATION_1 { get; set; }
        /// <summary>
        /// 缝合情况
        /// </summary>
        public string SUTURESITUATION_2 { get; set; }
        /// <summary>
        /// 缝合针数(单位：针)
        /// </summary>
        public string SEW { get; set; }
        /// <summary>
        /// 手术原因
        /// </summary>
        public string OPERATIONREASON { get; set; }
        /// <summary>
        /// 阴道分娩产后2h出血量（单位：ml）
        /// </summary>
        public string CHUXUE { get; set; }
        /// <summary>
        /// 手术人
        /// </summary>
        public string SSZXM { get; set; }
        /// <summary>
        /// 接生人
        /// </summary>
        public string ACCUSR { get; set; }
        /// <summary>
        /// 录入时间
        /// </summary>
        public string OPERATEDATE { get; set; }
        /// <summary>
        /// 录入单位机构代码
        /// </summary>
        public string ORG_1 { get; set; }
        /// <summary>
        /// 录入单位
        /// </summary>
        public string ORG_2 { get; set; }

        #region
        public string ZHICHANG { get; set; }//滞产：0否 1是
        public string ZHICHANGcode { get; set; }

        public string SALVE { get; set; } //危重抢救：0否 1是
        public string SALVEcode { get; set; }

        public string QJREASON { get; set; }//抢救原因

        public string ZHIRANFENMIAN { get; set; } //促进自然分娩措施编码
        public string ZHIRANFENMIANcode { get; set; }//促进自然分娩措施 1导乐陪伴 2 泡浴  3分娩球 4 水中分娩 5无痛分娩

        public string BAHENZIFENMIAN { get; set; }//疤痕子宫自然分娩：0否 1是
        public string BAHENZIFENMIANcode { get; set; }

        public string ZHIGONGPOLIE { get; set; } //子宫破裂：0无 1有
        public string ZHIGONGPOLIEcode { get; set; }

        public string ZHIGONGPOLIEYOU { get; set; } //发生院内外编码
        public string ZHIGONGPOLIEYOUcode { get; set; }

        public string YANGSHUIQUANSHUAN { get; set; } //羊水栓塞：0无 1有
        public string YANGSHUIQUANSHUANcode { get; set; }

        public string YANGSHUIQUANSHUANYOU { get; set; } //发生院内外编码
        public string YANGSHUIQUANSHUANYOUcode { get; set; }

        public string SSZZC { get; set; } //"职称编码
        public string SSZZCcode { get; set; }//手术者职称  1主任医师 2副主任医师 3主治医师 4医师

        public string BIRTHCERTIFICATENO { get; set; }//计划生育证明证件号

        public string INHOSPITAL { get; set; }//入院时间YYYY-MM-DD

        public string OUTHOSPITAL { get; set; }//出院时间YYYY-MM-DD

        public string YINDAOZHANGPAOFU { get; set; } //阴道试产转剖宫产 0否 1是
        public string YINDAOZHANGPAOFUcode { get; set; }

        public string CHANGHOUQIAN { get; set; }//产后血压（低）

        public string CHANGHOUHOU { get; set; }//产后血压（高）

        public string NEWWAYBIRTH { get; set; } //新法接生0否 1是
        public string NEWWAYBIRTHcode { get; set; }

        public string ACCORG { get; set; } //接生单位机构
        public string ACCORGcode { get; set; }//接生单位机构代码

        public string OPERATION { get; set; } //手术产情况 1臀助产 2臀牵引  3产钳 4胎头吸引
        public string OPERATIONcode { get; set; }

        public string PAUNCH { get; set; } //剖宫产指征代码
        public string PAUNCHcode { get; set; }

        public string OTHERPAUNCH { get; set; }//其他剖宫产指征

        public string TOGETHERILL { get; set; }//并发症或合并症
        public string TOGETHERILLcode { get; set; }//并发症或合并症代码

        public string TOGETHERILLOTHER { get; set; }//其他并发症或其他合并症

        public string BLEEDCAUSE { get; set; } //出血原因    1子宫收缩乏力  2软产道损伤  3胎盘胎膜残留 4疑血功能障碍
        public string BLEEDCAUSEcode { get; set; }

        public string LAOJIZHOU { get; set; }//绕颈几周

        public string ZHONGLIANG { get; set; }//重量：  g

        public string ZHONGDUZIXIAN { get; set; } //重度子痫前期： 0无 1有
        public string ZHONGDUZIXIANcode { get; set; }

        public string RELAXADDR { get; set; } //母亲产后休养地址
        public string RELAXADDRCODE { get; set; }//母亲产后休养地址行政区划代码

        public string VISITORG { get; set; } //访视机构
        public string VISITORGcode { get; set; }//访视机构代码

        public string POSTTEL { get; set; }//产后联系电话

        public string HDSB0101034 { get; set; } //建册人
        public string HDSB0101035 { get; set; } //建册时间
        #endregion

        /// <summary>
        /// 标志: 0 new; 1 update 
        /// </summary>
        public int flagId { get; set; }
        /// <summary>
        /// 婴儿
        /// </summary>
        public List<EntityChild> lstChild { get; set; }


    }
    #endregion

    #region Child
    /// <summary>
    /// Child
    /// </summary>
    public class EntityChild
    {
        /// <summary>
        /// 婴儿姓名
        /// </summary>
        public string BABYNAME { get; set; }
        /// <summary>
        /// 婴儿性别代码
        /// </summary>
        public string SEX_1 { get; set; }
        /// <summary>
        /// 婴儿性别
        /// </summary>
        public string SEX_2 { get; set; }
        /// <summary>
        /// 胎次
        /// </summary>
        public string SEQUENCE { get; set; }
        /// <summary>
        /// 出生时间
        /// </summary>
        public string DATEOFBIRTH { get; set; }
        /// <summary>
        /// 体重
        /// </summary>
        public string AVOIRDUPOIS { get; set; }
        /// <summary>
        /// 身长
        /// </summary>
        public string STATURE { get; set; }
        /// <summary>
        /// 头围
        /// </summary>
        public string TOUWEI { get; set; }

        /// <summary>
        /// 胸围
        /// </summary>
        public string XIONGWEI { get; set; }
        /// <summary>
        /// 新生儿出生情况  1活产 2死胎 3死产 4七天内新生儿死亡 9其他
        /// </summary>
        public string HEALTHcode { get; set; }
        /// <summary>
        /// 新生儿出生情况
        /// </summary>
        public string HEALTH { get; set; }
        /// <summary>
        /// 新生儿死亡 0-无1-早期死亡(<=7天死亡)  2-晚期死亡(>7天死亡)
        /// </summary>
        public string ISDEADcode { get; set; }
        /// <summary>
        /// 新生儿死亡
        /// </summary>
        public string ISDEAD { get; set; }
        /// <summary>
        /// 新生儿抢救 1-无 2-吸粘液 3-气管插管4-正压给氧 5-药物 6-其他
        /// </summary>
        public string HARDHELPcode { get; set; }
        /// <summary>
        /// 新生儿抢救
        /// </summary>
        public string HARDHELP { get; set; }
        /// <summary>
        /// 新生儿窒息0 否 1是
        /// </summary>
        public string NEWHUXIcode { get; set; }
        /// <summary>
        /// 新生儿窒息
        /// </summary>
        public string NEWHUXI { get; set; }
        /// <summary>
        /// 窒息程度 0重度  1其他
        /// </summary>
        public string NEWHUXIYOUcode { get; set; }
        /// <summary>
        /// 窒息程度
        /// </summary>
        public string NEWHUXIYOU { get; set; }
        /// <summary>
        /// 新生儿并发症 0 否 1是
        /// </summary>
        public string NEWILLcode { get; set; }
        /// <summary>
        /// 新生儿并发症
        /// </summary>
        public string NEWILL { get; set; }
        /// <summary>
        /// 新生儿吸入性肺炎 0 否 1是
        /// </summary>
        public string NEWFEIYANcode { get; set; }
        /// <summary>
        /// 新生儿吸入性肺炎
        /// </summary>
        public string NEWFEIYAN { get; set; }

        /// <summary>
        /// 新生儿破伤风 
        /// </summary>
        public string ISTETANUS { get; set; }
        /// <summary>
        /// 新生儿破伤风 0-未查 1-否 2-是
        /// </summary>
        public string ISTETANUScode { get; set; }
        /// <summary>
        /// 是否畸形代码
        /// </summary>
        public string ISBUG_1 { get; set; }
        /// <summary>
        /// 是否畸形
        /// </summary>
        public string ISBUG_2 { get; set; }
        /// <summary>
        /// 1min Apgar总分
        /// </summary>
        public string APGAR1 { get; set; }
        /// <summary>
        /// 5min Apgar总分
        /// </summary>
        public string APGAR5 { get; set; }
        /// <summary>
        /// 10min Apgar总分
        /// </summary>
        public string APGAR10 { get; set; }
        /// <summary>
        /// 是否注射乙肝免疫球蛋白代码
        /// </summary>
        public string HBIGTIME_1 { get; set; }
        /// <summary>
        /// 是否注射乙肝免疫球蛋白
        /// </summary>
        public string HBIGTIME_2 { get; set; }
        /// <summary>
        /// 注射日期
        /// </summary>
        public string INJECTIONDATE { get; set; }
        /// <summary>
        /// 注射剂量（单位：IU）
        /// </summary>
        public string JILIANG { get; set; }
        /// <summary>
        /// 产后30分钟内皮肤接触情况代码
        /// </summary>
        public string SKINCONTACT_1 { get; set; }
        /// <summary>
        /// 产后30分钟内皮肤接触情况
        /// </summary>
        public string SKINCONTACT_2 { get; set; }
    }
    #endregion

    #region
    public class EntityWacCheckRecord
    {
        public string patientId { get; set; }
        public string applicationId { get; set; }
        public string platgroupid { get; set; }
        public string platgroupname { get; set; }
        public string hisgroupid { get; set; }
        public string hisgroupname { get; set; }
        public string platitemid { get; set; }
        public string platitemname { get; set; }
        public string hisitemid { get; set; }
        public string hisitemname { get; set; }
        public DateTime uploaddate { get; set; }
        public string result { get; set; }
    }

    #endregion

    #endregion
}
