const sqlServerLinkInfoKey = "SQL_SERVER_LINK_INFO_KEY";
const mySqlLinkInfoKey = "MY_SQL_LINK_INFO_KEY";

function setItem(key, value) {
    window.localStorage.setItem(key, value);
}

function getItem(key) {
    return window.localStorage.getItem(key);
}

function removeItem(key) {
    window.localStorage.removeItem(key);
}

const vue = new Vue({
    el: '#app',
    data() {
        const account = (rule, value, callback) => {
            if (this.SQLServerForm.linkType === 'db') {
                if (value === '') {
                    callback(new Error('请输入数据库用户名'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        const pwd = (rule, value, callback) => {
            if (this.SQLServerForm.linkType === 'db') {
                if (value === '') {
                    callback(new Error('请输入数据库密码'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        const SNSID = (rule, value, callback) => {
            if (this.OracleForm.linkType === 'Basic') {
                if (value === '') {
                    callback(new Error('请输入Service Name/SID'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        const oraclePort = (rule, value, callback) => {
            if (this.OracleForm.linkType === 'Basic') {
                if (value === '') {
                    callback(new Error('请输入端口号'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };

        return {
            loading: false,
            activeIndex: 0,
            dbType: 0,
            dbName: ['SqlServer', 'MySQL', 'SQLite', 'Oracle', 'PostregSQL'],
            SQLServerForm: {
                host: '',
                linkType: 'db',
                account: '',
                pwd: '',
                db: ''
            },
            dbList: [],
            SQLServerFormRules: {
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                account: [
                    { validator: account, trigger: 'blur' }
                ],
                pwd: [
                    { validator: pwd, trigger: 'blur' }
                ],
                db: [
                    { required: true, message: '请选择数据库', trigger: 'blur' }
                ]
            },
            MySqlForm: {
                host: '',
                port: '3306',
                account: '',
                pwd: '',
                db: ''
            },
            MySqlFormRules: {
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { required: true, message: '请输入端口号', trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入登录帐号', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入登录密码', trigger: 'blur' }
                ]
            },
            SQLiteForm: {
                host: '',
                pwd: ''
            },
            SQLiteFormRules: {
                host: [
                    { required: true, message: '请输入DB文件地址或选择文件', trigger: 'blur' }
                ]
            },
            OracleForm: {
                host: '',
                port: '1521',
                linkType: 'Basic',
                account: '',
                pwd: '',
                SNSID: 'ORCL',
                radio: 'Service'
            },
            OracleFormRules: {
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { validator: oraclePort, trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入数据库用户名', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入数据库密码', trigger: 'blur' }
                ],
                SNSID: [
                    { validator: SNSID, trigger: 'blur' }
                ]
            },
            PGSqlForm: {
                host: '',
                port: '5432',
                account: '',
                pwd: '',
                db: ''
            },
            PGSqlFormRules: {
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { required: true, message: '请输入端口号', trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入登录帐号', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入登录密码', trigger: 'blur' }
                ],
                db: [
                    { required: true, message: '请输入要连接的数据库', trigger: 'blur' }
                ]
            },
            dbRadio: 'SqlServer',
            testIsSuccess: false,
            selectYuanDB: {
                dbType: '',
                linkString: ''
            },
            selectMubiaoDB: {
                dbType: '',
                linkString: ''
            },
            yuanTableData: [],
            yuanTableDataOld: [],
            mubiaoTableData: [],
            mubiaoTableDataOld: [],
            yuanTableDataSearch: '',
            mubiaoTableDataSearch: '',
            settingObj: {
                tableData: false,
                tableCover: false,
                tableAny: false,
                dataRows: 500
            },
            selectTables: []
        };
    },
    watch: {
        yuanTableDataSearch(val) {
            this.yuanTableDataSearchFunc();
        },
        mubiaoTableDataSearch(val) {
            this.mubiaoTableDataSearchFunc();
        }
    },
    methods: {
        tableRowClassName({ row, rowIndex }) {
            if (rowIndex % 2 === 1) {
                return 'warning-row';
            }
            return '';
        },
        leftDB() {
            let thisIndex = this.dbName.findIndex((value) => value === this.selectYuanDB.dbType);
            let itemIndex = this.dbType;
            if (this.dbType <= 0) {
                itemIndex = 4;
            } else {
                itemIndex--;
            }
            if (itemIndex === thisIndex) {
                itemIndex--;
            }
            if (itemIndex < 0) {
                itemIndex = 4;
            }
            this.dbType = itemIndex;
            this.dbRadio = this.dbName[this.dbType];
        },
        rightDB() {
            let thisIndex = this.dbName.findIndex((value) => value === this.selectYuanDB.dbType);
            let itemIndex = this.dbType;
            if (this.dbType >= 4) {
                itemIndex = 0;
            } else {
                itemIndex++;
            }
            if (itemIndex === thisIndex) {
                itemIndex++;
            }
            if (itemIndex > 4) {
                itemIndex = 0;
            }
            this.dbType = itemIndex;
            this.dbRadio = this.dbName[this.dbType];
        },
        dbRadioChange(label) {
            this.dbType = this.dbName.findIndex((value) => value === label);
        },
        selectDBFile() {
            sqlite.selectDBFile();
        },
        testSQLServerLink() {
            this.$refs['SQLServerForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    const json = JSON.stringify(this.SQLServerForm);
                    setItem(sqlServerLinkInfoKey, json);
                    if (this.SQLServerForm.linkType === 'win') {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Integrated Security=True`);
                    } else {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Persist Security Info=True;User ID=${this.SQLServerForm.account};Password=${this.SQLServerForm.pwd}`);
                    }
                }
            });
        },
        testMySqlLink() {
            this.$refs['MySqlForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    const json = JSON.stringify(this.MySqlForm);
                    setItem(mySqlLinkInfoKey, json);
                    let linkString = `server=${this.MySqlForm.host};User Id=${this.MySqlForm.account};password=${this.MySqlForm.pwd};port=${this.MySqlForm.port};SslMode = None;`;
                    mysql.testLink(linkString);
                }
            });
        },
        selectSqlServerDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['SQLServerForm'].validate((valid) => {
                if (valid) {
                    if (this.SQLServerForm.db === '') {
                        this.$message({
                            message: '请选择一个数据库',
                            type: 'warning'
                        });
                        return;
                    }
                    let linkString = '';
                    if (this.SQLServerForm.linkType === 'win') {
                        linkString = `Data Source=${this.SQLServerForm.host};Initial Catalog=${this.SQLServerForm.db};Integrated Security=True`;
                    } else {
                        linkString = `Data Source=${this.SQLServerForm.host};Initial Catalog=${this.SQLServerForm.db};Persist Security Info=True;User ID=${this.SQLServerForm.account};Password=${this.SQLServerForm.pwd}`;
                    }
                    if (this.activeIndex === 0) {
                        this.selectYuanDB.dbType = 'SqlServer';
                        this.selectYuanDB.linkString = linkString;
                        this.activeIndex = 1;
                        this.dbRadio = 'MySQL';
                        this.dbType = 1;
                        this.dbList = [];
                        this.testIsSuccess = false;
                    } else if (this.activeIndex === 1) {
                        this.selectMubiaoDB.dbType = 'SqlServer';
                        this.selectMubiaoDB.linkString = linkString;
                        this.activeIndex = 2;
                        this.testIsSuccess = false;
                    }
                }
            });
        },
        selectMySqlDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['MySqlForm'].validate((valid) => {
                if (valid) {
                    if (this.MySqlForm.db === '') {
                        this.$message({
                            message: '请选择一个数据库',
                            type: 'warning'
                        });
                        return;
                    }
                    let linkString = `server=${this.MySqlForm.host};User Id=${this.MySqlForm.account};password=${this.MySqlForm.pwd};Database=${this.MySqlForm.db};port=${this.MySqlForm.port};SslMode = None;`;
                    if (this.activeIndex === 0) {
                        this.selectYuanDB.dbType = 'MySQL';
                        this.selectYuanDB.linkString = linkString;
                        this.activeIndex = 1;
                        this.dbRadio = 'SqlServer';
                        this.dbType = 0;
                        this.dbList = [];
                        this.testIsSuccess = false;
                    } else if (this.activeIndex === 1) {
                        this.selectMubiaoDB.dbType = 'MySQL';
                        this.selectMubiaoDB.linkString = linkString;
                        this.activeIndex = 2;
                        this.testIsSuccess = false;
                    }
                }
            });
        },
        toStpe1() {
            this.dbRadio = 'SqlServer';
            this.dbType = 0;
            this.selectYuanDB.dbType = '';
            this.selectYuanDB.linkString = '';
            this.activeIndex = 0;
            this.testIsSuccess = false;
            this.dbList = [];
            this.yuanTableData = this.mubiaoTableData = this.yuanTableDataOld = this.mubiaoTableDataOld = [];
            this.yuanTableDataSearch = '';
            this.mubiaoTableDataSearch = '';
            this.selectTables = [];
        },
        toStpe2() {
            let thisIndex = this.dbName.findIndex((value) => value === this.selectYuanDB.dbType);
            if (thisIndex >= 1) {
                thisIndex--;
            } else {
                thisIndex = 1;
            }
            this.dbRadio = this.dbName[thisIndex];
            this.dbType = thisIndex;
            this.selectMubiaoDB.dbType = '';
            this.selectMubiaoDB.linkString = '';
            this.activeIndex = 1;
            this.testIsSuccess = false;
            this.dbList = [];
            this.yuanTableData = this.mubiaoTableData = this.yuanTableDataOld = this.mubiaoTableDataOld = [];
            this.yuanTableDataSearch = '';
            this.mubiaoTableDataSearch = '';
            this.selectTables = [];
        },
        loadingTablesYuan() {
            let objName;
            if (this.selectYuanDB.dbType === 'SqlServer') {
                objName = sqlServer;
            } else if (this.selectYuanDB.dbType === 'MySQL') {
                objName = mysql;
            }
            this.loading = true;
            objName.loadingTables(this.selectYuanDB.linkString, true);
        },
        loadingTablesMubiao() {
            let objName;
            if (this.selectMubiaoDB.dbType === 'SqlServer') {
                objName = sqlServer;
            } else if (this.selectMubiaoDB.dbType === 'MySQL') {
                objName = mysql;
            }
            this.loading = true;
            objName.loadingTables(this.selectMubiaoDB.linkString, false);
        },
        yuanTableDataRowClick(row, column, event) {
            this.$refs.yuanTableData.toggleRowSelection(row);
        },
        yuanTableDataSearchFunc() {
            if (this.yuanTableDataSearch) {
                this.yuanTableData = this.yuanTableDataOld.filter(val => val.TableName.toLowerCase().indexOf(this.yuanTableDataSearch.toLowerCase()) >= 0);
            } else {
                this.yuanTableData = this.yuanTableDataOld;
            }
        },
        mubiaoTableDataSearchFunc() {
            if (this.mubiaoTableDataSearch) {
                this.mubiaoTableData = this.mubiaoTableDataOld.filter(val => val.TableName.toLowerCase().indexOf(this.mubiaoTableDataSearch.toLowerCase()) >= 0);
            } else {
                this.mubiaoTableData = this.mubiaoTableDataOld;
            }
        },
        handleSelectionChange(val) {
            this.selectTables = val;
        },
        startMoveFunc() {
            if (this.selectTables && this.selectTables.length > 0) {
                move.startMove(JSON.stringify(this.selectTables), this.selectYuanDB.dbType, this.selectYuanDB.linkString, this.selectMubiaoDB.dbType, this.selectMubiaoDB.linkString);
            } else {
                this.$notify({
                    title: '迁移提示',
                    message: '请至少选择一个表进行迁移',
                    type: 'warning'
                });
            }
        }
    },
    created() {
        const sqlServerLinkInfo = getItem(sqlServerLinkInfoKey);
        if (sqlServerLinkInfo) {
            this.SQLServerForm = JSON.parse(sqlServerLinkInfo);
            this.SQLServerForm.db = '';
        }

        const mySqlLinkInfo = getItem(mySqlLinkInfoKey);
        if (mySqlLinkInfo) {
            this.MySqlForm = JSON.parse(mySqlLinkInfo);
            this.MySqlForm.db = '';
        }
    }
});

function testSuccessMsg() {
    vue.testIsSuccess = true;
    vue.$message({
        message: '测试连接成功, 正在读取数据库信息...',
        type: 'success'
    });
}

function hideLoading() {
    vue.loading = false;
}

function setDbList(json) {
    const dbList = JSON.parse(json);
    vue.dbList = dbList;
}

function setTables(json, propName) {
    vue[propName] = JSON.parse(json);
    vue[propName + 'Old'] = JSON.parse(json);
}