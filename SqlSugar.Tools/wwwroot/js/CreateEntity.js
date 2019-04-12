Vue.component('treeAppend', {
    template: '<span class="dbItem">\
    <span style="" v-if="data.node.level === 1">\
        <el-tooltip class="item" effect="dark" content="点击加载表列表" placement="right">\
            <el-button style="padding: 2px 4px;" icon="el-icon-refresh" type="success" size="mini" @click="$root.loadingTables(data.node.data)"></el-button>\
        </el-tooltip>\
        <el-tooltip class="item" effect="dark" content="点击生成所有表实体类" placement="right">\
            <el-button style="padding: 2px 4px;" icon="el-icon-download" type="success" size="mini" @click="$root.saveAllTables(data.node, data.node.data)"></el-button>\
        </el-tooltip>\
    </span>\
    <span style="" v-else>\
        <el-tooltip class="item" effect="dark" content="点击生成预览代码, 生成预览后按Ctrl + S可以保存" placement="right">\
            <el-button style="padding: 2px 4px;" icon="el-icon-view" type="success" size="mini" @click="$root.createOne(data.node, data.node.data)"></el-button>\
        </el-tooltip>\
    </span>\
    <span>\
        <el-tooltip class="item" effect="dark" :content="data.node.label" placement="top">\
            <span v-if="data.node.level === 1" style="font-size:14px;"><span style="padding:0px"></span>{{data.node.label}}</span>\
            <span v-else style="font-size:10px;"><span style="padding:0px"></span>{{data.node.label}}</span>\
        </el-tooltip>\
    </span>\
</span>',
    props: {
        data: {}
    }
});

const vue = new Vue({
    el: "#app",
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

        return {
            filterText: '',
            showSettingsDialog: false,
            testIsSuccess: false,
            loading: false,
            activeIndex: '',
            showSQLServerDialog: false,
            showSQLiteDialog: false,
            SQLServerForm: {
                name: '',
                host: '',
                linkType: 'db',
                account: '',
                pwd: '',
                db: ''
            },
            dbList: [],
            SQLServerFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
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
            dbData: [],
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            thisNodeData: null,
            settingsForm: {
                namespace: '',
                entityNamespace: 'Entitys',
                baseClassName: '',
                classCapsCount: 0,
                propCapsCount: 0,
                propTrim: false,
                propDefault: false,
                sqlSugarPK: false,
                sqlSugarBZL: false,
                getCus: 'return this._属性;',
                setCus: 'this._属性 = -value-;',
                cusAttr: '',
                cusGouZao: '',
                propType: '1'
            },
            createOneParam: {
                node: null,
                data: null
            },
            createOneSuccess: false,

            SQLiteForm: {
                name: '',
                host: '',
                pwd: ''
            },
            SQLiteFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入DB文件地址或选择文件', trigger: 'blur' }
                ]
            },
            showMySqlDialog: false,
            MySqlForm: {
                name: '',
                host: '',
                port: '3306',
                account: '',
                pwd: '',
                db: ''
            },
            MySqlFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
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
            showPGSqlDialog: false,
            PGSqlForm: {
                name: '',
                host: '',
                port: '5432',
                account: '',
                pwd: '',
                db: ''
            },
            PGSqlFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
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
            showManagerDialog: false,

            showOracleDialog: false,
            OracleForm: {
                name: '',
                host: '',
                port: '1521',
                linkType: 'Basic',
                account: '',
                pwd: '',
                SNSID: 'ORCL',
                radio: 'Service'
            },
            OracleFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
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
            }
        };
    },
    watch: {
        filterText(val) {
            this.$refs.tree.filter(val);
        }
    },
    methods: {
        filterNode(value, data) {
            if (!value) return true;
            return data.label.indexOf(value) !== -1;
        },
        handleSelect(key, keyPath) {
            this.activeIndex = key;
            if (key === "1") {
                this.showSQLServerDialog = true;
            } else if (key === "6") {
                this.showSettingsDialog = true;
            } else if (key === "5") {
                this.showSQLiteDialog = true;
            } else if (key === "2") {
                this.showMySqlDialog = true;
            } else if (key === "4") {
                this.showPGSqlDialog = true;
            } else if (key === "7") {
                this.showManagerDialog = true;
            } else if (key === "3") {
                this.showOracleDialog = true;
            }
        },
        SQLServerDialogClosed() {
            this.SQLServerForm.name = '';
            this.SQLServerForm.host = '';
            this.SQLServerForm.linkType = 'db';
            this.SQLServerForm.account = '';
            this.SQLServerForm.pwd = '';
            this.SQLServerForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['SQLServerForm'].clearValidate();
        },
        renderContent: function (h, e) {
            return h('treeAppend', {
                props: {
                    data: e
                }
            });
        },
        testSQLServerLink() {
            this.$refs['SQLServerForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    if (this.SQLServerForm.linkType === 'win') {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Integrated Security=True`);
                    } else {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Persist Security Info=True;User ID=${this.SQLServerForm.account};Password=${this.SQLServerForm.pwd}`);
                    }
                }
            });
        },
        selectDB() {
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
                    this.dbData.push({ label: this.SQLServerForm.name, linkString, children: [], type: 'sqlserver' });
                    this.showSQLServerDialog = false;
                    addedDBData({ label: this.SQLServerForm.name, linkString, children: [], type: 'sqlserver' });
                }
            });
        },
        loadingTables(dbInfo) {
            this.loading = true;
            this.thisNodeData = dbInfo;
            if (dbInfo.type === 'sqlserver') {
                sqlServer.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'sqlite') {
                sqlite.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'mysql') {
                mysql.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'pgsql') {
                pgsql.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'oracle') {
                oracle.loadingTables(dbInfo.linkString);
            }
        },
        settingsSave() {
            const settingsJson = JSON.stringify(this.settingsForm);
            window.localStorage.setItem("settingsJson", settingsJson);
            this.showSettingsDialog = false;
        },
        createOne(node, data) {
            this.loading = true;
            this.createOneParam.node = node;
            this.createOneParam.data = data;
            const linkString = node.parent.data.linkString;
            const tableDesc = data.TableDesc;
            const tableName = data.label;
            const info = JSON.stringify({
                linkString,
                tableDesc,
                tableName,
                settings: JSON.stringify(this.settingsForm)
            });
            if (node.parent.data.type === 'sqlserver') {
                sqlServer.createOne(info);
            } else if (node.parent.data.type === 'sqlite') {
                sqlite.createOne(info);
            } else if (node.parent.data.type === 'mysql') {
                mysql.createOne(info);
            } else if (node.parent.data.type === 'pgsql') {
                pgsql.createOne(info);
            } else if (node.parent.data.type === 'oracle') {
                oracle.createOne(info);
            }
        },
        saveOneCode() {
            if (this.createOneSuccess && this.createOneParam.node !== null && this.createOneParam.data !== null) {
                this.loading = true;
                const linkString = this.createOneParam.node.parent.data.linkString;
                const tableDesc = this.createOneParam.data.TableDesc;
                const tableName = this.createOneParam.data.label;
                const info = JSON.stringify({
                    linkString,
                    tableDesc,
                    tableName,
                    settings: JSON.stringify(this.settingsForm)
                });
                if (this.createOneParam.node.parent.data.type === 'sqlserver') {
                    sqlServer.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'sqlite') {
                    sqlite.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'mysql') {
                    mysql.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'pgsql') {
                    pgsql.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'oracle') {
                    oracle.saveOne(info);
                }
            }
        },
        saveAllTables(node, data) {
            if (data.children.length <= 0) {
                this.$message({
                    message: '该数据库没有表或您还没有加载表列表',
                    type: 'warning'
                });
                return;
            }
            this.loading = true;
            const linkString = node.data.linkString;
            const info = JSON.stringify({
                linkString,
                settings: JSON.stringify(this.settingsForm),
                tableList: JSON.stringify(data.children)
            });
            if (node.data.type === 'sqlserver') {
                sqlServer.saveAllTables(info);
            } else if (node.data.type === 'sqlite') {
                sqlite.saveAllTables(info);
            } else if (node.data.type === 'mysql') {
                mysql.saveAllTables(info);
            } else if (node.data.type === 'pgsql') {
                pgsql.saveAllTables(info);
            } else if (node.data.type === 'oracle') {
                oracle.saveAllTables(info);
            }
        },
        SQLiteDialogClosed() {
            this.SQLiteForm.name = '';
            this.SQLiteForm.host = '';
            this.SQLiteForm.pwd = '';
            this.testIsSuccess = false;
            this.$refs['SQLiteForm'].clearValidate();
        },
        selectDBFile() {
            sqlite.selectDBFile();
        },
        testSQLiteLink() {
            this.$refs['SQLiteForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    if (this.SQLiteForm.pwd !== '') {
                        sqlite.testLink(`Data Source=${this.SQLiteForm.host};Password=${this.SQLiteForm.pwd};`);
                    } else {
                        sqlite.testLink(`Data Source=${this.SQLiteForm.host};`);
                    }
                }
            });
        },
        selectSQLiteDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['SQLiteForm'].validate((valid) => {
                if (valid) {
                    let linkString = '';
                    if (this.SQLiteForm.pwd !== '') {
                        linkString = `Data Source=${this.SQLiteForm.host};Password=${this.SQLiteForm.pwd};`;
                    } else {
                        linkString = `Data Source=${this.SQLiteForm.host};`;
                    }
                    this.dbData.push({ label: this.SQLiteForm.name, linkString, children: [], type: 'sqlite' });
                    this.showSQLiteDialog = false;
                    addedDBData({ label: this.SQLiteForm.name, linkString, children: [], type: 'sqlite' });
                }
            });
        },
        mySqlDialogClosed() {
            this.MySqlForm.name = '';
            this.MySqlForm.host = '';
            this.MySqlForm.port = '3306';
            this.MySqlForm.account = '';
            this.MySqlForm.pwd = '';
            this.MySqlForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['MySqlForm'].clearValidate();
        },
        testMySqlLink() {
            this.$refs['MySqlForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let linkString = `server=${this.MySqlForm.host};User Id=${this.MySqlForm.account};password=${this.MySqlForm.pwd};port=${this.MySqlForm.port};SslMode = None;`;
                    mysql.testLink(linkString);
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
                    this.dbData.push({ label: this.MySqlForm.name, linkString, children: [], type: 'mysql' });
                    this.showMySqlDialog = false;
                    addedDBData({ label: this.MySqlForm.name, linkString, children: [], type: 'mysql' });
                }
            });
        },
        PGSqlDialogClosed() {
            this.PGSqlForm.name = '';
            this.PGSqlForm.host = '';
            this.PGSqlForm.port = '5432';
            this.PGSqlForm.account = '';
            this.PGSqlForm.pwd = '';
            this.PGSqlForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['PGSqlForm'].clearValidate();
        },
        testPGSqlLink() {
            this.$refs['PGSqlForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let linkString = `Host=${this.PGSqlForm.host};Port=${this.PGSqlForm.port};Username=${this.PGSqlForm.account};Password=${this.PGSqlForm.pwd};Database=${this.PGSqlForm.db};`;
                    pgsql.testLink(linkString);
                }
            });
        },
        selectPGSqlDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['PGSqlForm'].validate((valid) => {
                if (valid) {
                    let linkString = `Host=${this.PGSqlForm.host};Port=${this.PGSqlForm.port};Username=${this.PGSqlForm.account};Password=${this.PGSqlForm.pwd};Database=${this.PGSqlForm.db};`;
                    this.dbData.push({ label: this.PGSqlForm.name, linkString, children: [], type: 'pgsql' });
                    this.showPGSqlDialog = false;
                    addedDBData({ label: this.PGSqlForm.name, linkString, children: [], type: 'pgsql' });
                }
            });
        },
        deleteDB(index, name) {
            this.$confirm(`确定删除名为: [${name}] 的数据库连接吗?`, '删除提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                deleteDBData(index);
                this.dbData.splice(index, 1);
                this.$message({
                    type: 'success',
                    message: '删除成功!'
                });
            }).catch(() => {
            });
        },
        editDB(index, name) {
            this.$prompt('请输入新的连接名称', `修改 [${name}] 的连接名称`, {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputPlaceholder: '请输入新的连接名称哦~',
                inputValidator: (val) => {
                    if (!val) {
                        return '请输入连接名称';
                    }
                    return true;
                }
            }).then(({ value }) => {
                let dbData = getDBData();
                dbData[index].label = value;
                window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
                this.dbData[index].label = value;
                this.$message({
                    type: 'success',
                    message: '编辑成功!'
                });
            }).catch(() => {
            });
        },
        OracleDialogClosed() {
            this.$refs['OracleForm'].resetFields();
            this.OracleForm.radio = 'Service';
            this.OracleForm.linkType = 'Basic';
            this.OracleForm.host = '';
        },
        testOracleLink() {
            if (this.OracleForm.linkType === 'TNS') {
                this.$message({
                    message: '暂时不支持TNS连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['OracleForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let itemService = '';
                    if (this.OracleForm.radio === 'Service') {
                        itemService = 'SERVICE_NAME';
                    } else {
                        itemService = 'SID';
                    }
                    let linkString = `Password=${this.OracleForm.pwd};User ID=${this.OracleForm.account};Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=${this.OracleForm.host})(PORT=${this.OracleForm.port})))(CONNECT_DATA=(SERVER=DEDICATED)(${itemService}=${this.OracleForm.SNSID})));`;
                    oracle.testLink(linkString);
                }
            });
        },
        selectOracleDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['OracleForm'].validate((valid) => {
                if (valid) {
                    let itemService = '';
                    if (this.OracleForm.radio === 'Service') {
                        itemService = 'SERVICE_NAME';
                    } else {
                        itemService = 'SID';
                    }
                    let linkString = `Password=${this.OracleForm.pwd};User ID=${this.OracleForm.account};Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=${this.OracleForm.host})(PORT=${this.OracleForm.port})))(CONNECT_DATA=(SERVER=DEDICATED)(${itemService}=${this.OracleForm.SNSID})));`;
                    this.showOracleDialog = false;
                    this.dbData.push({ label: this.OracleForm.name, linkString, children: [], type: 'oracle' });
                    addedDBData({ label: this.OracleForm.name, linkString, children: [], type: 'oracle' });
                }
            });
        }
    },
    created() {
        this.dbData = getDBData();
        const settingsJson = window.localStorage.getItem("settingsJson");
        if (settingsJson !== undefined && settingsJson !== null && settingsJson !== "") {
            const settingsObject = JSON.parse(settingsJson);
            this.settingsForm.namespace = settingsObject.namespace;
            this.settingsForm.entityNamespace = settingsObject.entityNamespace;
            this.settingsForm.baseClassName = settingsObject.baseClassName;
            this.settingsForm.classCapsCount = settingsObject.classCapsCount;
            this.settingsForm.propCapsCount = settingsObject.propCapsCount;
            this.settingsForm.propTrim = settingsObject.propTrim;
            this.settingsForm.propDefault = settingsObject.propDefault;
            this.settingsForm.sqlSugarPK = settingsObject.sqlSugarPK;
            this.settingsForm.sqlSugarBZL = settingsObject.sqlSugarBZL;
            this.settingsForm.getCus = settingsObject.getCus;
            this.settingsForm.setCus = settingsObject.setCus;
            this.settingsForm.cusAttr = settingsObject.cusAttr;
            this.settingsForm.cusGouZao = settingsObject.cusGouZao;
            this.settingsForm.propType = settingsObject.propType;
        }
    }
});

function addedDBData(dbInfo) {
    let dbData = getDBData();
    dbData.push(dbInfo);
    window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
}

function deleteDBData(index) {
    let dbData = getDBData();
    dbData.splice(index, 1);
    window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
}

function getDBData() {
    let json = localStorage.getItem('dbDataKey');
    if (json === undefined || json === null || json === "") {
        return [];
    }
    return JSON.parse(json);
}

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

function setTables(json) {
    vue.dbData[vue.dbData.indexOf(vue.thisNodeData)].children = JSON.parse(json);
}

function getEntityCode(code) {
    document.getElementById("code").innerHTML = code;
    vue.createOneSuccess = true;
}

function saveOneSuccess() {
    vue.$message({
        message: '保存成功',
        type: 'success'
    });
}

function saveAllTablesSuccess() {
    vue.$message({
        message: '导出所有表成功',
        type: 'success'
    });
}

function setSQLiteFilePath(path) {
    vue.SQLiteForm.host = path;
}