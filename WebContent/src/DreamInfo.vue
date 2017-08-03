<template>
  <div>
    <transition name="fade">
      <div>
        <template v-if="isloaded">
          <div>
            <el-row>
              <el-col :span="10">
                <img :src="avater" class="avater"/>
              </el-col>
              <el-col :span="14">
                <el-form label-position="right">
                  <el-form-item label="记本">
                    <span>{{title}}</span>
                  </el-form-item>
                  <el-form-item label="介绍">
                    <span>{{content}}</span>
                  </el-form-item>
                  <el-form-item label="进展">
                    <span>{{step + '个'}}</span>
                  </el-form-item>
                </el-form>
              </el-col>
            </el-row>
          </div>
          <el-table :data="steps">
            <el-table-column type="expand">
              <template scope="props">
                <el-form inline class="demo-table-expand">
                  <el-form-item label="进展">
                    <span>{{ props.row.content }}</span>
                  </el-form-item>
                </el-form>
                <CommentInfo :dreamid="dreamid" :stepid="props.row.id"></CommentInfo>
              </template>
            </el-table-column>
            <el-table-column
              label="进展ID"
              prop="id"
              align="left">
            </el-table-column>
            <el-table-column
              label="进展"
              prop="content"
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
        </template>
      </div>
    </transition>
  </div>
</template>

<script>
  import axios from 'axios';
  import CommentInfo from './CommentInfo';
  export default {
    name: 'DreamInfo',
    props: ['dreamid'],
    data () {
      return {
        steps: [],
        isloaded: true,
        avater: '',
        title: '',
        content: '',
        step: ''
      };
    },
    created: function () {
      var _context = this;
      axios.get('http://localhost:8081/Nian/' + this.dreamid)
        .then(function (response) {
          var resp = response.data.data;
          console.log(resp);
          _context.steps = resp.steps;
          _context.avater = 'http://img.nian.so/dream/' + resp.image;
          _context.title = resp.title;
          _context.content = resp.content;
          _context.step = resp.step;
          _context.isLoaded = true;
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
      },
      contextformatter: function (row, col, value) {
        console.log(value);
        if (value.length > 50) {
          return value.slice(0, 47) + '...';
        }
        return value;
      }
    },
    components: {CommentInfo}
  };
</script>

<style>
  .avater
  {
    max-width: 150px;
    max-height: 150px;
  }
  .fade-enter-active, .fade-leave-active {
    transition: opacity .75s
  }
  .fade-enter, .fade-leave-to /* .fade-leave-active in below version 2.1.8 */ {
    opacity: 0
  }
</style>
