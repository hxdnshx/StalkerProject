<template>
  <div>
    <transition name="fade">
        <el-table :data="steps">
          <el-table-column
            label="评论"
            prop="content"
            align="left">
          </el-table-column>
          <el-table-column
            label="作者"
            prop="user"
            align="left"
            :formatter="contextformatter">
          </el-table-column>
          <el-table-column
            label="状态"
            prop="isremoved"
            align="left"
            :formatter="privformatter">
          </el-table-column>
        </el-table>
    </transition>
  </div>
</template>

<script>
  import axios from 'axios';
  import consts from './GlobalConst.vue';
  export default {
    name: 'CommentInfo',
    props: ['dreamid', 'stepid'],
    data () {
      return {
        comments: []
      };
    },
    created: function () {
      var _context = this;
      axios.get(consts.serverpath + '/' + this.dreamid + '/' + this.stepid)
        .then(function (response) {
          var resp = response.data.data;
          // console.log(resp);
          _context.comments = resp.comment;
        });
    },
    methods: {
      privformatter: function (row, col, value) {
        if (value === false) {
          return '正常';
        }
        if (value === true) {
          return '已删除';
        }
        return 'しらない';
      }
    }
  };
</script>

<style>

</style>
