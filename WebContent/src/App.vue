<template>
  <div id="app" class="page">
    <el-button v-on:click="loaddata">刷新数据</el-button>
    <template v-if="dataloaded">
      <el-row>
        <el-col :span="10">
          <img :src="avater" class="avater"/>
        </el-col>
        <el-col :span="14">
          <el-form inline>
            <el-form-item label="用户名">
              <span>{{name}}</span>
            </el-form-item>
          </el-form>
        </el-col>
      </el-row>
    </template>
    <el-row>
      <el-col :span="24">
        <el-table :data="dreams">
          <el-table-column type="expand">
            <template scope="props">
              <DreamInfo :dreamid="props.row.id"></DreamInfo>
            </template>
          </el-table-column>
          <el-table-column
            label="梦想ID"
            prop="id"
            align="left">
          </el-table-column>
          <el-table-column
            label="梦想名"
            prop="title"
            align="left">
          </el-table-column>
          <el-table-column
            label="梦想状态"
            prop="private"
            :formatter="privformatter"
            align="left">
          </el-table-column>
        </el-table>
      </el-col>
    </el-row>
  </div>
</template>

<script>
  import axios from 'axios';
  import DreamInfo from './DreamInfo';
export default {
    name: 'app',
    data () {
      return {
        dreams: [],
        name: '',
        uid: '',
        dataloaded: false
      };
    },
    created: function () {
      this.loaddata(null);
    },
    methods: {
      loaddata: function (event) {
        var _context = this;
        axios.get('http://localhost:8081/Nian')
          .then(function (response) {
            var resp = response.data.data;
            console.log(resp.dreams);
            console.log(_context);
            _context.dreams = resp.dreams;
            _context.name = resp.name;
            _context.uid = resp.uid;
            _context.dataloaded = true;
          });
      },
      privformatter: function (row, col, value) {
        if (value === '0') {
          return '公开';
        }
        if (value === '1') {
          return '私密';
        }
        if (value === '2') {
          return '已删除';
        }
        return 'しらない';
      }
    },
    computed: {
      avater: function () {
        return 'http://img.nian.so/head/' + this.uid + '.jpg';
      }
    },
    filters: {
      privatestate: function (value) {
        if (value === '0') {
          return '公开';
        }
        if (value === '1') {
          return '私密';
        }
        if (value === '2') {
          return '已删除';
        }
        return 'しらない';
      }
    },
    components: {DreamInfo}
};
</script>

<style>
  #app {
    font-family: 'Avenir', Helvetica, Arial, sans-serif;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    text-align: center;
    color: #2c3e50;
    margin-top: 60px;
    overflow : auto;
  }
  .page {
    max-width : 1140px;
    padding : 30px;
    margin : auto;
    display : block;
  }
  .el-row {
    position : relative;
  }
  .el-table-column {
    align : left;
  }
  .avater{
    max-width:200px;
    max-height:200px;
  }
</style>
